namespace MothersonBoxManagement.Entities;

public class Box
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
    public int CreatedByUserId { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int? CompletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionMode { get; set; }
    public string? ExceptionReason { get; set; }
    public string? BlockReason { get; set; }
    public int? BlockedByUserId { get; set; }
    public DateTime? BlockedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User CreatedBy { get; set; } = null!;
    public User? LastModifiedBy { get; set; }
    public User? CompletedBy { get; set; }
    public User? BlockedBy { get; set; }
    public ICollection<BoxPackage> Packages { get; set; } = new List<BoxPackage>();
}
