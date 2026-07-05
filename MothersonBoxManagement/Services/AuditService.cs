using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogScanRejectionAsync(int? boxId, string barcode, string reason, int userId, string workstationName, CancellationToken ct = default)
    {
        var log = new BoxAuditLog
        {
            BoxId = boxId,
            UserId = userId,
            ActionType = "PackageRejected",
            Timestamp = DateTime.UtcNow,
            WorkstationName = workstationName,
            PackageBarcode = barcode,
            Reason = reason,
            Description = $"Scan rejected for package {barcode}. Reason: {reason}"
        };

        _context.BoxAuditLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }
}
