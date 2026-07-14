using System.Diagnostics;
using Microsoft.Win32;

namespace MothersonBoxManagement.PrintAgent;

internal static class AgentInstaller
{
    private const string RunValueName = "MothersonPrintAgent";

    public static bool InstallIfNeeded(string[] args)
    {
        var current = Environment.ProcessPath ?? Application.ExecutablePath;
        var isSetup = args.Contains("--install", StringComparer.OrdinalIgnoreCase) ||
            Path.GetFileNameWithoutExtension(current).EndsWith("Setup", StringComparison.OrdinalIgnoreCase);
        if (!isSetup || string.Equals(Path.GetFullPath(current), Path.GetFullPath(AgentPaths.ExecutablePath), StringComparison.OrdinalIgnoreCase))
            return false;

        Directory.CreateDirectory(AgentPaths.DirectoryPath);
        File.Copy(current, AgentPaths.ExecutablePath, true);
        SetAutoStart();
        Process.Start(new ProcessStartInfo(AgentPaths.ExecutablePath, "--configure") { UseShellExecute = true });
        return true;
    }

    public static void SetAutoStart()
    {
        using var run = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        run.SetValue(RunValueName, $"\"{AgentPaths.ExecutablePath}\"");
    }

    public static void Uninstall()
    {
        using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true))
            run?.DeleteValue(RunValueName, throwOnMissingValue: false);
        if (File.Exists(AgentPaths.ConfigurationPath))
            File.Delete(AgentPaths.ConfigurationPath);
        MessageBox.Show("Autostart and local credentials were removed. You can now delete the Motherson PrintAgent folder.", "Print Agent");
    }
}
