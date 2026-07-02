using MothersonBoxManagement.Data.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxService
{
    Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId);
    Task<List<BoxListItemDto>> GetOpenBoxesAsync();
    Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode);
    Task<BoxDetailsDto?> GetBoxByIdAsync(int id);
    Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId);
}
