using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Services;

public interface IPackageScanService
{
    Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, string workstationName, CancellationToken cancellationToken = default, string? requestId = null);
}
