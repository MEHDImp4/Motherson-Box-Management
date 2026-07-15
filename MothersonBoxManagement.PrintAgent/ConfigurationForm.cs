using MothersonBoxManagement.PrintAgent.Core;

namespace MothersonBoxManagement.PrintAgent;

internal sealed class ConfigurationForm : Form
{
    private readonly TextBox _server = new() { PlaceholderText = "https://motherson-server", Dock = DockStyle.Fill };
    private readonly TextBox _code = new() { PlaceholderText = "12-character pairing code", CharacterCasing = CharacterCasing.Upper, MaxLength = 12, Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private readonly Button _pair = new() { Text = "Pair this workstation", AutoSize = true };
    public bool Paired { get; private set; }

    public ConfigurationForm()
    {
        Text = "Motherson Print Agent configuration";
        Width = 520;
        Height = ConfigurationDialogLayout.MinimumWindowHeight;
        MinimumSize = new Size(520, ConfigurationDialogLayout.MinimumWindowHeight);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 6
        };
        for (var row = 0; row < layout.RowCount; row++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Server URL", AutoSize = true });
        layout.Controls.Add(_server);
        layout.Controls.Add(new Label { Text = "Temporary pairing code", AutoSize = true });
        layout.Controls.Add(_code);
        layout.Controls.Add(_status);
        layout.Controls.Add(_pair);
        Controls.Add(layout);
        _pair.Click += PairClicked;
        AcceptButton = _pair;
        var existing = AgentConfiguration.Load();
        if (existing is not null) _server.Text = existing.ServerUrl;
    }

    private async void PairClicked(object? sender, EventArgs eventArgs)
    {
        _pair.Enabled = false;
        _status.Text = "Pairing…";
        try
        {
            using var api = new PrintAgentApiClient();
            var result = await api.PairAsync(_server.Text, _code.Text, CancellationToken.None);
            AgentConfiguration.Save(_server.Text, result.WorkstationCode, result.Token);
            AgentInstaller.SetAutoStart();
            Paired = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            _status.Text = "Pairing failed. Check HTTPS URL, code, network and system clock.";
        }
        finally { _pair.Enabled = true; }
    }
}
