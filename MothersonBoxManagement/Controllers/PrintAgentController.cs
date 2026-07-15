using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;
using MothersonBoxManagement.Security;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public sealed partial class PrintAgentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPrintAgentService _service;
    private readonly IWebHostEnvironment _environment;

    public PrintAgentController(ApplicationDbContext db, IPrintAgentService service, IWebHostEnvironment environment)
    {
        _db = db;
        _service = service;
        _environment = environment;
    }

    [HttpGet("PrintAgent/Status")]
    public async Task<IActionResult> Status(string workstationName, CancellationToken cancellationToken)
    {
        var station = NormalizeStation(workstationName);
        if (station is null)
            return BadRequest(new { error = "WORKSTATION_REQUIRED" });
        var workstation = await _db.PrinterConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Code == station || candidate.PcName == station, cancellationToken);
        if (workstation is null)
            return Ok(new { configured = false, workstationName = station, download = ArtifactMetadata() });

        var printers = DeserializePrinters(workstation.AvailablePrintersJson);
        var isOnline = workstation.LastSeenAt >= DateTime.UtcNow.AddMinutes(-1);
        var printerAvailable = printers.Contains(workstation.PrinterName, StringComparer.OrdinalIgnoreCase);
        var queueCounts = await _db.BoxPrintJobs.AsNoTracking()
            .Where(job => job.WorkstationId == workstation.Id)
            .GroupBy(job => job.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var recentJobs = await _db.BoxPrintJobs.AsNoTracking()
            .Where(job => job.WorkstationId == workstation.Id)
            .OrderByDescending(job => job.RequestedAt)
            .Take(10)
            .Select(job => new { job.Id, job.Status, job.RequestedAt, job.PrintedAt, job.ErrorMessage, job.RetryCount, BoxNumber = job.Box.BoxNumber })
            .ToListAsync(cancellationToken);
        var recentPrintedJobs = await _db.BoxPrintJobs.AsNoTracking()
            .Where(job => job.WorkstationId == workstation.Id && job.Status == PrintJobStatuses.Printed)
            .OrderByDescending(job => job.PrintedAt)
            .Take(5)
            .Select(job => new { job.Id, BoxNumber = job.Box.BoxNumber, job.PrintedAt, job.PrinterName })
            .ToListAsync(cancellationToken);
        return Ok(new
        {
            configured = true,
            workstationId = workstation.Id,
            workstationName = workstation.Code,
            workstation.MachineName,
            workstation.PrinterName,
            workstation.PrintMode,
            workstation.AgentVersion,
            workstation.LastSeenAt,
            isOnline,
            printerAvailable,
            printers,
            queue = new
            {
                pending = queueCounts.FirstOrDefault(item => item.Status == PrintJobStatuses.Pending)?.Count ?? 0,
                claimed = queueCounts.FirstOrDefault(item => item.Status == PrintJobStatuses.Claimed)?.Count ?? 0,
                failed = queueCounts.FirstOrDefault(item => item.Status == PrintJobStatuses.Failed)?.Count ?? 0,
                cancelled = queueCounts.FirstOrDefault(item => item.Status == PrintJobStatuses.Cancelled)?.Count ?? 0
            },
            recentJobs,
            recentPrintedJobs,
            download = ArtifactMetadata()
        });
    }

    [HttpGet("PrintAgent/History")]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public async Task<IActionResult> History(string workstationName, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var station = NormalizeStation(workstationName);
        if (station is null) return BadRequest(new { error = "WORKSTATION_REQUIRED" });
        var workstation = await _db.PrinterConfigurations.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Code == station || candidate.PcName == station, cancellationToken);
        if (workstation is null) return NotFound();
        pageSize = Math.Clamp(pageSize, 5, 100);
        page = Math.Max(1, page);
        var query = _db.BoxPrintJobs.AsNoTracking().Where(job => job.WorkstationId == workstation.Id && job.Status == PrintJobStatuses.Printed);
        var total = await query.CountAsync(cancellationToken);
        var jobs = await query.OrderByDescending(job => job.PrintedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(job => new { job.Id, BoxNumber = job.Box.BoxNumber, job.PrintedAt, job.PrinterName }).ToListAsync(cancellationToken);
        return Ok(new { jobs, page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("PrintAgent/Fleet")]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public async Task<IActionResult> Fleet(CancellationToken cancellationToken)
    {
        var stations = await _db.PrinterConfigurations.AsNoTracking().OrderBy(item => item.Code).ToListAsync(cancellationToken);
        var activities = await _db.BoxAuditLogs.AsNoTracking().Where(item => item.WorkstationName != null)
            .OrderByDescending(item => item.Timestamp).Select(item => new { item.WorkstationName, item.ActionType, item.Timestamp, UserName = item.User.FullName }).ToListAsync(cancellationToken);
        var jobs = await _db.BoxPrintJobs.AsNoTracking().GroupBy(item => item.WorkstationId)
            .Select(group => new { WorkstationId = group.Key, Pending = group.Count(item => item.Status == PrintJobStatuses.Pending), Claimed = group.Count(item => item.Status == PrintJobStatuses.Claimed), Failed = group.Count(item => item.Status == PrintJobStatuses.Failed) }).ToListAsync(cancellationToken);
        return Ok(stations.Select(station =>
        {
            var activity = activities.FirstOrDefault(item => item.WorkstationName == station.Code || item.WorkstationName == station.PcName);
            var queue = jobs.FirstOrDefault(item => item.WorkstationId == station.Id);
            return new { station.Id, station.Code, station.DisplayName, station.PcName, station.MachineName, station.PrinterName, station.PrintMode, station.AgentVersion, station.LastSeenAt, station.LastIpAddress, Printers = DeserializePrinters(station.AvailablePrintersJson), IsOnline = station.LastSeenAt >= DateTime.UtcNow.AddMinutes(-1), Pending = queue?.Pending ?? 0, Claimed = queue?.Claimed ?? 0, Failed = queue?.Failed ?? 0, LastAction = activity?.ActionType, LastActionAt = activity?.Timestamp, LastUser = activity?.UserName };
        }));
    }

    [HttpGet("PrintAgent/Workstations")]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public IActionResult Workstations() => View();

    [HttpPost("PrintAgent/Workstations/{id:int}/Configure")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public async Task<IActionResult> ConfigureWorkstation(int id, string? displayName, string printerName, string printMode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(printerName) || printerName.Trim().Length > 200 || !PrintModes.IsValid(printMode))
            return BadRequest();
        if (displayName?.Trim().Length > 100)
            return BadRequest();
        var workstation = await _db.PrinterConfigurations.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workstation is null) return NotFound();
        var printers = DeserializePrinters(workstation.AvailablePrintersJson);
        if (!printers.Contains(printerName.Trim(), StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "PRINTER_NOT_REPORTED" });
        workstation.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        workstation.PrinterName = printers.First(name => string.Equals(name, printerName.Trim(), StringComparison.OrdinalIgnoreCase));
        workstation.PrintMode = printMode;
        workstation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Remote workstation configuration saved.";
        return RedirectToAction(nameof(Workstations));
    }

    [HttpPost("PrintAgent/PairingCode")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public async Task<IActionResult> PairingCode(string workstationName, CancellationToken cancellationToken)
    {
        var station = NormalizeStation(workstationName);
        if (station is null)
            return BadRequest(new { error = "WORKSTATION_REQUIRED" });
        var workstation = await _db.PrinterConfigurations.FirstOrDefaultAsync(
            candidate => candidate.Code == station || candidate.PcName == station, cancellationToken);
        if (workstation is null)
        {
            workstation = new PrinterConfiguration
            {
                Code = station,
                PcName = station,
                PrintMode = PrintModes.Windows,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.PrinterConfigurations.Add(workstation);
            await _db.SaveChangesAsync(cancellationToken);
        }
        var code = await _service.CreatePairingCodeAsync(workstation.Id, cancellationToken);
        return Ok(new { code, expiresAt = DateTime.UtcNow.AddMinutes(10) });
    }

    [HttpPost("PrintAgent/Configure")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public async Task<IActionResult> Configure(string workstationName, string printerName, string printMode, CancellationToken cancellationToken)
    {
        var station = NormalizeStation(workstationName);
        if (station is null || string.IsNullOrWhiteSpace(printerName) || printerName.Trim().Length > 200 || !PrintModes.IsValid(printMode))
            return BadRequest();
        var workstation = await _db.PrinterConfigurations.FirstOrDefaultAsync(
            candidate => candidate.Code == station || candidate.PcName == station, cancellationToken);
        if (workstation is null)
            return NotFound();
        var available = DeserializePrinters(workstation.AvailablePrintersJson);
        if (!available.Contains(printerName.Trim(), StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "PRINTER_NOT_REPORTED" });

        workstation.PrinterName = available.First(name => string.Equals(name, printerName.Trim(), StringComparison.OrdinalIgnoreCase));
        workstation.PrintMode = printMode;
        workstation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Print-agent configuration saved.";
        return RedirectToAction("Settings", "Box");
    }

    [HttpPost("PrintAgent/Revoke")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public async Task<IActionResult> Revoke(string workstationName, CancellationToken cancellationToken)
    {
        var station = NormalizeStation(workstationName);
        var workstation = station is null ? null : await _db.PrinterConfigurations.FirstOrDefaultAsync(
            candidate => candidate.Code == station || candidate.PcName == station, cancellationToken);
        if (workstation is null)
            return NotFound();
        workstation.AgentTokenHash = null;
        workstation.RevokedAt = DateTime.UtcNow;
        workstation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return RedirectToAction("Settings", "Box");
    }

    [HttpPost("PrintAgent/Retry/{jobId:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public async Task<IActionResult> Retry(int jobId, CancellationToken cancellationToken)
    {
        var job = await _db.BoxPrintJobs.FirstOrDefaultAsync(candidate => candidate.Id == jobId, cancellationToken);
        if (job is null)
            return NotFound();
        job.Status = PrintJobStatuses.Pending;
        job.RetryCount = 0;
        job.ErrorMessage = null;
        job.FailedAt = null;
        job.NextAttemptAt = DateTime.UtcNow;
        job.LeaseTokenHash = null;
        job.LeaseExpiresAt = null;
        job.ReprintReason = "Administrator retry";
        await _db.SaveChangesAsync(cancellationToken);
        return RedirectToAction("Settings", "Box");
    }

    [HttpGet("Downloads/PrintAgent")]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public IActionResult Download()
    {
        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "Downloads", "MothersonPrintAgentSetup.exe");
        if (!System.IO.File.Exists(path))
            return NotFound("The print-agent artifact is not present in this build.");
        return PhysicalFile(path, "application/vnd.microsoft.portable-executable", "MothersonPrintAgentSetup.exe");
    }

    private object ArtifactMetadata()
    {
        var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "Downloads");
        var exePath = Path.Combine(directory, "MothersonPrintAgentSetup.exe");
        var metadataPath = Path.Combine(directory, "MothersonPrintAgentSetup.json");
        if (!System.IO.File.Exists(exePath))
            return new { available = false };
        if (System.IO.File.Exists(metadataPath))
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(System.IO.File.ReadAllText(metadataPath));
            }
            catch (JsonException) { }
        }
        return new { available = true, version = "development", sha256 = string.Empty };
    }

    private static string[] DeserializePrinters(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string? NormalizeStation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = UnsafeStationCharacters().Replace(value.Trim(), string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized[..Math.Min(normalized.Length, 64)];
    }

    [GeneratedRegex("[^A-Za-z0-9 _.-]")]
    private static partial Regex UnsafeStationCharacters();
}
