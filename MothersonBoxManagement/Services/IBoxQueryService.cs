using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxQueryService
{
    Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto?> FindBoxByPackageBarcodeAsync(string packageBarcode, CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> GetCreatedBoxesAsync(CancellationToken cancellationToken = default);
    Task<List<BoxListItemDto>> SearchBoxesAsync(BoxSearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<PagedResult<BoxListItemDto>> GetOpenBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<BoxListItemDto>> GetCreatedBoxesPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
