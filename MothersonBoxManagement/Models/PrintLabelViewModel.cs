namespace MothersonBoxManagement.Models;

public class PrintLabelViewModel
{
    public string BoxNumber { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public string QrCodeBase64 { get; set; } = string.Empty;
    public bool AutoPrint { get; set; }
    public string? ReturnUrl { get; set; }
}
