using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Dtos;

public class BoxListItemDto
{
    public int Id { get; set; }
    public string BoxNumber { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public BoxType Type { get; set; }
    public int ExpectedQuantity { get; set; }
    public int CurrentQuantity { get; set; }
    public BoxStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string CreatedByMatricule { get; set; } = string.Empty;
    public DateTime LastModifiedAt { get; set; }
    public string LastUserMatricule { get; set; } = string.Empty;
}
