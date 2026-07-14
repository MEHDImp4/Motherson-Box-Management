namespace MothersonBoxManagement.Entities;

public class BoxPrintJob
{
    public int Id { get; set; }
    public int BoxId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public string? PrinterUncPath { get; set; }
    public string? Payload { get; set; }
    public string? PayloadType { get; set; }
    public int RequestedByUserId { get; set; }
    public int? WorkstationId { get; set; }
    public int RetryCount { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? ReprintReason { get; set; }
    public int PayloadVersion { get; set; } = 1;
    public string? LeaseTokenHash { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Box Box { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public PrinterConfiguration? Workstation { get; set; }
}
