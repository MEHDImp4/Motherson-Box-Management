namespace MothersonBoxManagement.PrintAgent.Core;

public static class AgentVersionFormatter
{
    public const int MaximumLength = 40;

    public static string ForApi(string? version)
    {
        var normalized = string.IsNullOrWhiteSpace(version) ? "unknown" : version.Trim();
        return normalized[..Math.Min(normalized.Length, MaximumLength)];
    }
}
