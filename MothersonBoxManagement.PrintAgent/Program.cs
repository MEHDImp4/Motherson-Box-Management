namespace MothersonBoxManagement.PrintAgent;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, "Local\\MothersonBoxManagement.PrintAgent", out var created);
        if (!created)
        {
            MessageBox.Show("Motherson Print Agent is already running.", "Print Agent");
            return;
        }

        if (AgentInstaller.InstallIfNeeded(args))
            return;

        Application.Run(new PrintAgentContext(args.Contains("--configure", StringComparer.OrdinalIgnoreCase)));
    }
}
