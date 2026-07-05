namespace MothersonBoxManagement.Entities;

public class BoxAuditLog
{
    public int Id { get; set; }
    public int? BoxId { get; set; }
    public int? RelatedBoxId { get; set; }
    public string? PackageBarcode { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? WorkstationName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public string? DetailsJson { get; set; }

    public Box? Box { get; set; }
    public Box? RelatedBox { get; set; }
    public User User { get; set; } = null!;
}
