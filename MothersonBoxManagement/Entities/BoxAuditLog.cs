namespace MothersonBoxManagement.Entities;

public class BoxAuditLog
{
    public int Id { get; set; }
    public int? BoxId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? WorkstationName { get; set; }
    public string? DetailsJson { get; set; }

    public Box? Box { get; set; }
    public User User { get; set; } = null!;
}
