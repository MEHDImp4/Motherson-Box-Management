using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Printing;

public sealed class PrintAgentService : IPrintAgentService
{
    private static readonly HashSet<string> AllowedErrors = new(StringComparer.Ordinal)
    {
        "PRINTER_UNAVAILABLE", "PAPER_OUT", "SPOOLER_ERROR", "INVALID_PAYLOAD", "UNKNOWN"
    };

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public PrintAgentService(ApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<string> CreatePairingCodeAsync(int workstationId, CancellationToken cancellationToken = default)
    {
        var workstationExists = await _db.PrinterConfigurations.AnyAsync(
            candidate => candidate.Id == workstationId && candidate.IsActive, cancellationToken);
        if (!workstationExists)
            throw new KeyNotFoundException("Workstation not found.");

        var now = _clock.GetUtcNow().UtcDateTime;
        var oldCodes = await _db.PrintAgentPairingCodes
            .Where(candidate => candidate.WorkstationId == workstationId && candidate.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var oldCode in oldCodes)
            oldCode.ConsumedAt = now;

        var rawCode = PrintAgentSecurity.CreatePairingCode();
        _db.PrintAgentPairingCodes.Add(new PrintAgentPairingCode
        {
            WorkstationId = workstationId,
            CodeHash = PrintAgentSecurity.Hash(rawCode),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(10)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return rawCode;
    }

    public async Task<PairAgentResult> PairAsync(string code, string machineName, string agentVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 12)
            throw new ArgumentException("Invalid pairing code.", nameof(code));
        machineName = RequireBounded(machineName, 100, "machineName");
        agentVersion = RequireBounded(agentVersion, 40, "agentVersion");
        var now = _clock.GetUtcNow().UtcDateTime;
        var hash = PrintAgentSecurity.Hash(code.Trim().ToUpperInvariant());
        var pairing = await _db.PrintAgentPairingCodes
            .Include(candidate => candidate.Workstation)
            .FirstOrDefaultAsync(candidate => candidate.CodeHash == hash, cancellationToken);
        if (pairing is null || pairing.ConsumedAt is not null || pairing.ExpiresAt <= now || !pairing.Workstation.IsActive)
            throw new InvalidOperationException("The pairing code is invalid or expired.");

        var token = PrintAgentSecurity.CreateToken();
        pairing.ConsumedAt = now;
        pairing.Workstation.AgentTokenHash = PrintAgentSecurity.Hash(token);
        pairing.Workstation.MachineName = machineName;
        pairing.Workstation.AgentVersion = agentVersion;
        pairing.Workstation.LastSeenAt = now;
        pairing.Workstation.RevokedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return new PairAgentResult(pairing.WorkstationId, pairing.Workstation.Code, token);
    }

    public async Task<PrinterConfiguration?> AuthenticateAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return null;
        var hash = PrintAgentSecurity.Hash(token);
        return await _db.PrinterConfigurations.FirstOrDefaultAsync(candidate =>
            candidate.AgentTokenHash == hash && candidate.IsActive && candidate.RevokedAt == null,
            cancellationToken);
    }

    public async Task HeartbeatAsync(int workstationId, AgentHeartbeat heartbeat, CancellationToken cancellationToken = default)
    {
        var workstation = await GetWorkstationAsync(workstationId, cancellationToken);
        workstation.MachineName = RequireBounded(heartbeat.MachineName, 100, "machineName");
        workstation.AgentVersion = RequireBounded(heartbeat.AgentVersion, 40, "agentVersion");
        workstation.AvailablePrintersJson = JsonSerializer.Serialize(heartbeat.Printers
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Where(name => name.Length <= 200)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Take(100));
        workstation.LastSeenAt = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BoxPrintJob> QueueAsync(int boxId, int requestedByUserId, int workstationId, string? reprintReason = null, CancellationToken cancellationToken = default)
    {
        var box = await _db.Boxes.FirstOrDefaultAsync(candidate => candidate.Id == boxId, cancellationToken)
            ?? throw new KeyNotFoundException("Box not found.");
        var workstation = await GetWorkstationAsync(workstationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(workstation.PrinterName) || !PrintModes.IsValid(workstation.PrintMode))
            throw new InvalidOperationException("The workstation printer is not configured.");

        var payload = new PrintLabelPayload(1, box.BoxNumber, box.BarcodeValue);
        var now = _clock.GetUtcNow().UtcDateTime;
        var job = new BoxPrintJob
        {
            BoxId = boxId,
            RequestedByUserId = requestedByUserId,
            WorkstationId = workstationId,
            PrinterName = workstation.PrinterName,
            PrinterUncPath = workstation.PrinterUncPath,
            Payload = JsonSerializer.Serialize(payload),
            PayloadType = workstation.PrintMode,
            PayloadVersion = 1,
            Status = PrintJobStatuses.Pending,
            RequestedAt = now,
            NextAttemptAt = now,
            ReprintReason = string.IsNullOrWhiteSpace(reprintReason) ? null : reprintReason.Trim()[..Math.Min(reprintReason.Trim().Length, 500)]
        };
        _db.BoxPrintJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<ClaimedPrintJob?> ClaimNextAsync(int workstationId, bool ignoreRetryDelay = false, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var job = await _db.BoxPrintJobs
            .Where(candidate => candidate.WorkstationId == workstationId &&
                (candidate.Status == PrintJobStatuses.Pending ||
                 (candidate.Status == PrintJobStatuses.Claimed && candidate.LeaseExpiresAt <= now)) &&
                (ignoreRetryDelay || candidate.NextAttemptAt == null || candidate.NextAttemptAt <= now))
            .OrderBy(candidate => candidate.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
            return null;

        var leaseToken = PrintAgentSecurity.CreateToken();
        job.Status = PrintJobStatuses.Claimed;
        job.StartedAt = now;
        job.LeaseTokenHash = PrintAgentSecurity.Hash(leaseToken);
        job.LeaseExpiresAt = now.AddMinutes(1);
        await _db.SaveChangesAsync(cancellationToken);
        var payload = JsonSerializer.Deserialize<PrintLabelPayload>(job.Payload ?? string.Empty)
            ?? throw new InvalidOperationException("The print payload is invalid.");
        return new ClaimedPrintJob(job.Id, job.Status, leaseToken, job.PrinterName, job.PayloadType ?? PrintModes.Windows, payload);
    }

    public async Task CompleteAsync(int workstationId, int jobId, string leaseToken, CancellationToken cancellationToken = default)
    {
        var job = await GetClaimedJobAsync(workstationId, jobId, leaseToken, cancellationToken);
        job.Status = PrintJobStatuses.Printed;
        job.PrintedAt = _clock.GetUtcNow().UtcDateTime;
        ClearLease(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(int workstationId, int jobId, string leaseToken, string errorCode, bool transient, CancellationToken cancellationToken = default)
    {
        var job = await GetClaimedJobAsync(workstationId, jobId, leaseToken, cancellationToken);
        job.RetryCount++;
        job.ErrorMessage = AllowedErrors.Contains(errorCode) ? errorCode : "UNKNOWN";
        var now = _clock.GetUtcNow().UtcDateTime;
        if (transient && job.RetryCount < 3)
        {
            job.Status = PrintJobStatuses.Pending;
            job.NextAttemptAt = now.AddSeconds(job.RetryCount switch { 1 => 5, 2 => 30, _ => 120 });
        }
        else
        {
            job.Status = PrintJobStatuses.Failed;
            job.FailedAt = now;
            job.NextAttemptAt = null;
        }
        ClearLease(job);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PrinterConfiguration> GetWorkstationAsync(int id, CancellationToken cancellationToken) =>
        await _db.PrinterConfigurations.FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.IsActive, cancellationToken)
        ?? throw new KeyNotFoundException("Workstation not found.");

    private async Task<BoxPrintJob> GetClaimedJobAsync(int workstationId, int jobId, string leaseToken, CancellationToken cancellationToken)
    {
        var leaseHash = PrintAgentSecurity.Hash(leaseToken);
        return await _db.BoxPrintJobs.FirstOrDefaultAsync(candidate =>
            candidate.Id == jobId && candidate.WorkstationId == workstationId &&
            candidate.Status == PrintJobStatuses.Claimed && candidate.LeaseTokenHash == leaseHash,
            cancellationToken) ?? throw new InvalidOperationException("The print job lease is invalid.");
    }

    private static void ClearLease(BoxPrintJob job)
    {
        job.LeaseTokenHash = null;
        job.LeaseExpiresAt = null;
    }

    private static string RequireBounded(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"Invalid {field}.", field);
        return value.Trim();
    }
}
