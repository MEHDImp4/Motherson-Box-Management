namespace MothersonBoxManagement.Entities;

public class PasswordResetRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public string? RequestedFromIp { get; set; }
    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
