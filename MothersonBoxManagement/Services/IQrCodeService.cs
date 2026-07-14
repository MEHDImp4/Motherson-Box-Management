namespace MothersonBoxManagement.Services;

public interface IQrCodeService
{
    byte[] GenerateQrCodePng(string content, int pixelsPerModule = 10);
    string GenerateQrCodeBase64(string content, int pixelsPerModule = 10);
}
