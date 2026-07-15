namespace MothersonBoxManagement.Printing;

public static class PrintModes
{
    public const string Windows = "Windows";
    public const string Zpl = "Zpl";
    public static bool IsValid(string? value) => value is Windows or Zpl;
}

public static class PrintJobStatuses
{
    public const string Pending = "Pending";
    public const string Claimed = "Claimed";
    public const string Printed = "Printed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public sealed record PrintLabelPayload(
    int Version,
    string BoxNumber,
    string BarcodeValue,
    int WidthDots = 800,
    int HeightDots = 800,
    int Dpi = 203,
    int Copies = 1);

public sealed record PairAgentResult(int WorkstationId, string WorkstationCode, string Token);

public sealed record ClaimedPrintJob(
    int Id,
    string Status,
    string LeaseToken,
    string PrinterName,
    string PrintMode,
    PrintLabelPayload Payload);

public sealed record AgentHeartbeat(
    string MachineName,
    string AgentVersion,
    IReadOnlyCollection<string> Printers,
    string? RemoteIpAddress = null);
