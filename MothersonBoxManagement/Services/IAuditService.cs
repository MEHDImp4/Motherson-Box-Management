using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Models;

namespace MothersonBoxManagement.Services;

public interface IAuditService
{
    Task LogScanRejectionAsync(int? boxId, string barcode, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<AuditIndexViewModel> GetAuditLogsAsync(AuditFilterDto filter, CancellationToken ct = default);
}
