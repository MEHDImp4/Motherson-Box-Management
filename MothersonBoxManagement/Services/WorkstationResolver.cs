namespace MothersonBoxManagement.Services;

using System.Text.RegularExpressions;

public interface IWorkstationResolver
{
    string Resolve(string? clientWorkstationName = null);
}

public class WorkstationResolver : IWorkstationResolver
{
    private const int MaxWorkstationNameLength = 64;
    private static readonly Regex UnsafeCharacters = new("[^A-Za-z0-9 _.-]", RegexOptions.Compiled);
    private readonly IConfiguration _configuration;

    public WorkstationResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Resolve(string? clientWorkstationName = null)
    {
        var clientName = Normalize(clientWorkstationName);
        if (!string.IsNullOrEmpty(clientName))
        {
            return clientName;
        }

        string? configured = _configuration["WorkstationName"];
        if (string.IsNullOrWhiteSpace(configured) || configured == "DEV-STATION-01" || configured == "DEFAULT-STATION")
        {
            return Environment.MachineName;
        }

        return Normalize(configured) ?? Environment.MachineName;
    }

    private static string? Normalize(string? workstationName)
    {
        if (string.IsNullOrWhiteSpace(workstationName))
        {
            return null;
        }

        var normalized = UnsafeCharacters.Replace(workstationName.Trim(), string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        normalized = normalized.Trim();
        return normalized.Length > MaxWorkstationNameLength ? normalized[..MaxWorkstationNameLength] : normalized;
    }
}
