using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Services;

public interface IBarcodeService
{
    Task<string> GenerateUniqueBoxBarcodeAsync(CancellationToken cancellationToken = default);
    bool IsBoxBarcode(string barcode);
    bool IsPackageBarcode(string barcode);
    void ValidateBarcodeFormat(string barcode, bool expectBox);
}
