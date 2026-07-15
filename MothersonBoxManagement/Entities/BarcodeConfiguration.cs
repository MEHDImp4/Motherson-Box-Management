namespace MothersonBoxManagement.Entities;

public class BarcodeConfiguration
{
    public const int DefaultId = 1;

    public int Id { get; set; }
    public string BoxPrefix { get; set; } = "BOX-";
    public string BoxDatePattern { get; set; } = "yyyyMMdd";
    public int BoxRandomLength { get; set; } = 6;
    public string? PackagePrefix { get; set; }
    public int PackageMinLength { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
