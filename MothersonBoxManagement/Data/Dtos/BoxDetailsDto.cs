using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data.Dtos;

public class BoxDetailsDto
{
    public int Id { get; set; }
    public string BoxNumber { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public BoxType Type { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Depth { get; set; }
    public int ExpectedQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public BoxStatus Status { get; set; }
    public string CreatedByMatricule { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionMode { get; set; }
    public string? ExceptionReason { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public List<PackageItemDto> Packages { get; set; } = new();
}

public class PackageItemDto
{
    public int Id { get; set; }
    public string PackageBarcode { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public string ScannedByMatricule { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
}
