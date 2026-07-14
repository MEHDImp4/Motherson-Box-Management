using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxPackageService
{
    Task<BoxDetailsDto> BlockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> TransferPackageAsync(int packageId, int destinationBoxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> RetraitPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> DisassociatePackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
}
