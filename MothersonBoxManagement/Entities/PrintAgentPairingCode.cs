namespace MothersonBoxManagement.Entities;

public sealed class PrintAgentPairingCode
{
    public int Id { get; set; }
    public int WorkstationId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public PrinterConfiguration Workstation { get; set; } = null!;
}
