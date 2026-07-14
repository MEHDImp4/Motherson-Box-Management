namespace MothersonBoxManagement.PrintAgent;

internal static class AgentPaths
{
    public static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Motherson", "PrintAgent");
    public static readonly string ExecutablePath = Path.Combine(DirectoryPath, "MothersonPrintAgent.exe");
    public static readonly string ConfigurationPath = Path.Combine(DirectoryPath, "agent.json");
}
