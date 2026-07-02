using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data.Dtos;

public class BoxDetailsDto
{
    public int Id { get; set; }
    public string BoxNumber { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public BoxType Type { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public int Depth { get; set; }
    public int ExpectedQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public BoxStatus Status { get; set; }
    public string CreatedByMatricule { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<PackageItemDto> Packages { get; set; } = new();
}

public class PackageItemDto
{
    public int Id { get; set; }
    public string PackageBarcode { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public string ScannedByMatricule { get; set; } = string.Empty;
}
