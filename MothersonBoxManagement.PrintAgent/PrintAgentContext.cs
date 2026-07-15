using MothersonBoxManagement.PrintAgent.Core;

namespace MothersonBoxManagement.PrintAgent;

internal sealed class PrintAgentContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly PrintAgentApiClient _api = new();
    private Task? _worker;
    private int _networkFailures;

    public PrintAgentContext(bool forceConfigure)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Configure / pair", null, (_, _) => Configure());
        menu.Items.Add("Reconnect now", null, (_, _) => RestartWorker());
        menu.Items.Add("Uninstall", null, (_, _) => Uninstall());
        menu.Items.Add("Exit", null, (_, _) => Exit());
        _icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "Motherson Print Agent - starting",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => Configure();

        if (forceConfigure || AgentConfiguration.Load() is null)
            Configure();
        RestartWorker();
    }

    private void Configure()
    {
        using var form = new ConfigurationForm();
        if (form.ShowDialog() == DialogResult.OK)
            RestartWorker();
    }

    private void RestartWorker()
    {
        var config = AgentConfiguration.Load();
        if (config is null)
        {
            SetStatus("Not paired");
            return;
        }
        try { _api.Configure(config); }
        catch { SetStatus("Invalid configuration"); return; }
        if (_worker is null || _worker.IsCompleted)
            _worker = RunAsync(_shutdown.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var nextHeartbeat = DateTime.MinValue;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow >= nextHeartbeat)
                {
                    await _api.HeartbeatAsync(LabelPrinter.InstalledPrinters(), cancellationToken);
                    nextHeartbeat = DateTime.UtcNow.AddSeconds(30);
                }
                var job = await _api.NextAsync(cancellationToken);
                _networkFailures = 0;
                if (job is null)
                {
                    SetStatus("Online - waiting for labels");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    continue;
                }
                SetStatus($"Printing job {job.Id}");
                try
                {
                    LabelPrinter.Print(job);
                    await _api.CompleteAsync(job, cancellationToken);
                    SetStatus($"Printed job {job.Id}");
                }
                catch (PrinterAgentException exception)
                {
                    await _api.FailAsync(job, exception.ErrorCode, exception.Transient, cancellationToken);
                    SetStatus($"Print failed: {exception.ErrorCode}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch
            {
                _networkFailures++;
                SetStatus("Offline - reconnecting");
                await Task.Delay(AgentBackoff.ForFailureCount(_networkFailures - 1), cancellationToken);
            }
        }
    }

    private void SetStatus(string value)
    {
        if (_icon is null) return;
        var text = $"Motherson Print Agent - {value}";
        _icon.Text = text[..Math.Min(text.Length, 63)];
    }

    private void Uninstall()
    {
        if (MessageBox.Show("Remove autostart and local agent credentials?", "Print Agent", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        AgentInstaller.Uninstall();
        Exit();
    }

    private void Exit()
    {
        _shutdown.Cancel();
        _icon.Visible = false;
        _icon.Dispose();
        _api.Dispose();
        ExitThread();
    }
}
