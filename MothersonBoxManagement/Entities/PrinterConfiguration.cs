namespace MothersonBoxManagement.Entities;

public class PrinterConfiguration
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PcName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public string PrinterUncPath { get; set; } = string.Empty;
    public string PrintMode { get; set; } = "Windows";
    public string? MachineName { get; set; }
    public string? AgentTokenHash { get; set; }
    public string? AgentVersion { get; set; }
    public string AvailablePrintersJson { get; set; } = "[]";
    public DateTime? LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
