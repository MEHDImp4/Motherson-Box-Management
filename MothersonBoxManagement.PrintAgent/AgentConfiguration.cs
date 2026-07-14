using System.Text.Json;

namespace MothersonBoxManagement.PrintAgent;

internal sealed class AgentConfiguration
{
    public string ServerUrl { get; set; } = string.Empty;
    public string WorkstationCode { get; set; } = string.Empty;
    public string ProtectedToken { get; set; } = string.Empty;
    public string Token => Dpapi.Unprotect(ProtectedToken);

    public static AgentConfiguration? Load()
    {
        if (!File.Exists(AgentPaths.ConfigurationPath)) return null;
        try { return JsonSerializer.Deserialize<AgentConfiguration>(File.ReadAllText(AgentPaths.ConfigurationPath)); }
        catch { return null; }
    }

    public static void Save(string serverUrl, string workstationCode, string token)
    {
        Directory.CreateDirectory(AgentPaths.DirectoryPath);
        var config = new AgentConfiguration
        {
            ServerUrl = serverUrl.TrimEnd('/'),
            WorkstationCode = workstationCode,
            ProtectedToken = Dpapi.Protect(token)
        };
        File.WriteAllText(AgentPaths.ConfigurationPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}
