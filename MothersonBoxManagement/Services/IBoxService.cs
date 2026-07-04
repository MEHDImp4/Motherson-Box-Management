using System.Threading;
using System.Threading.Tasks;
using MothersonBoxManagement.Data.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxService
{
    Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, CancellationToken cancellationToken = default);

    // Box exception methods
    Task<BoxDetailsDto> CancelBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> ForceCloseBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> UpdateExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> BlockBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockBoxAsync(int boxId, string reason, int userId, CancellationToken ct = default);

    // Package exception methods
    Task<BoxDetailsDto> BlockPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> TransferPackageAsync(int packageId, int destinationBoxId, string reason, int userId, CancellationToken ct = default);
    Task<BoxDetailsDto> RetraitPackageAsync(int packageId, string reason, int userId, CancellationToken ct = default);
}
