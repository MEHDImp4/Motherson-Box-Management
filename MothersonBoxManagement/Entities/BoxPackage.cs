namespace MothersonBoxManagement.Entities;

public class BoxPackage
{
    public int Id { get; set; }
    public int BoxId { get; set; }
    public string PackageBarcode { get; set; } = string.Empty;
    public int ScannedByUserId { get; set; }
    public DateTime ScannedAt { get; set; }
    public string? WorkstationName { get; set; }
    public bool IsBlocked { get; set; } = false;
    public string? BlockReason { get; set; }
    public bool IsRemoved { get; set; }
    public DateTime? RemovedAt { get; set; }
    public int? RemovedByUserId { get; set; }
    public string? RemovalReason { get; set; }
    public string? ScanRequestId { get; set; }

    public Box Box { get; set; } = null!;
    public User ScannedBy { get; set; } = null!;
    public User? RemovedBy { get; set; }
}
