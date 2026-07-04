namespace MothersonBoxManagement.Entities;

public class Box
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
    public int CreatedByUserId { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ExceptionReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User CreatedBy { get; set; } = null!;
    public User? LastModifiedBy { get; set; }
    public User? ClosedBy { get; set; }
    public ICollection<BoxPackage> Packages { get; set; } = new List<BoxPackage>();
}
