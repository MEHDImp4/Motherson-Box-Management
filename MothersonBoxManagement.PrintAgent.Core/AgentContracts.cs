namespace MothersonBoxManagement.PrintAgent.Core;

public sealed record AgentPrintLabelPayload(
    int Version,
    string BoxNumber,
    string BarcodeValue,
    int WidthDots,
    int HeightDots,
    int Dpi,
    int Copies);

public sealed record AgentClaimedPrintJob(
    int Id,
    string Status,
    string LeaseToken,
    string PrinterName,
    string PrintMode,
    AgentPrintLabelPayload Payload);

public sealed record AgentPairResult(int WorkstationId, string WorkstationCode, string Token);

public static class AgentBackoff
{
    public static TimeSpan ForFailureCount(int failureCount)
    {
        var normalized = Math.Clamp(failureCount, 0, 30);
        return TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, normalized + 1)));
    }
}
