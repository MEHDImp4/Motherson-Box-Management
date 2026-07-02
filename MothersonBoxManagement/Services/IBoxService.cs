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
}
