using QRCoder;

namespace MothersonBoxManagement.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GenerateQrCodePng(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrPng = new PngByteQRCode(qrCodeData);
        return qrPng.GetGraphic(pixelsPerModule);
    }

    public string GenerateQrCodeBase64(string content, int pixelsPerModule = 10)
    {
        var pngBytes = GenerateQrCodePng(content, pixelsPerModule);
        return Convert.ToBase64String(pngBytes);
    }
}
