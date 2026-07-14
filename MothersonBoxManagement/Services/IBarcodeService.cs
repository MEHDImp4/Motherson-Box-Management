using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Services;

public interface IBarcodeService
{
    Task<string> GenerateUniqueBoxBarcodeAsync(CancellationToken cancellationToken = default);
    bool IsBoxBarcode(string barcode);
    Task<bool> IsBoxBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    bool IsPackageBarcode(string barcode);
    Task<bool> IsPackageBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    void ValidateBarcodeFormat(string barcode, bool expectBox);
    Task ValidateBarcodeFormatAsync(string barcode, bool expectBox, CancellationToken cancellationToken = default);
}
