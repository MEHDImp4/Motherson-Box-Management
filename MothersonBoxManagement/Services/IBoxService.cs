using System.Threading;
using System.Threading.Tasks;
using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxService
{
    Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> GetCreatedBoxesAsync(CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> SearchBoxesAsync(BoxSearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> FindBoxByPackageBarcodeAsync(string packageBarcode, CancellationToken cancellationToken = default);
    Task<PagedResult<BoxListItemDto>> GetOpenBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<BoxListItemDto>> GetCreatedBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    // Box lifecycle methods
    Task<BoxDetailsDto> OpenBoxAsync(int boxId, int userId, string workstationName, CancellationToken ct = default);

    // Box exception methods
    Task<BoxDetailsDto> CancelBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> ForceCloseBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UpdateExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> BlockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> ArchiveBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);

    // Package exception methods
    Task<BoxDetailsDto> BlockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> TransferPackageAsync(int packageId, int destinationBoxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> RetraitPackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> DisassociatePackageAsync(int packageId, string reason, int userId, string workstationName, CancellationToken ct = default);
}
