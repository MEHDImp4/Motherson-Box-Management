using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using MothersonBoxManagement.PrintAgent.Core;

namespace MothersonBoxManagement.PrintAgent;

internal sealed class PrintAgentApiClient : IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<AgentPairResult> PairAsync(string serverUrl, string code, CancellationToken cancellationToken)
    {
        var baseUri = ValidateServerUrl(serverUrl);
        using var response = await _client.PostAsJsonAsync(new Uri(baseUri, "api/print-agent/pair"), new
        {
            code,
            machineName = Environment.MachineName,
            agentVersion = AgentVersionFormatter.ForApi(Application.ProductVersion)
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentPairResult>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Pairing response was empty.");
    }

    public void Configure(AgentConfiguration config)
    {
        _client.BaseAddress = ValidateServerUrl(config.ServerUrl);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Token);
    }

    public async Task HeartbeatAsync(IReadOnlyCollection<string> printers, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync("api/print-agent/heartbeat", new
        {
            machineName = Environment.MachineName,
            agentVersion = AgentVersionFormatter.ForApi(Application.ProductVersion),
            printers,
            ipAddress = ResolveLanIpv4Address()
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AgentClaimedPrintJob?> NextAsync(CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync("api/print-agent/jobs/next", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentClaimedPrintJob>(JsonOptions, cancellationToken);
    }

    public async Task CompleteAsync(AgentClaimedPrintJob job, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync($"api/print-agent/jobs/{job.Id}/complete", new { job.LeaseToken }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task FailAsync(AgentClaimedPrintJob job, string errorCode, bool transient, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync($"api/print-agent/jobs/{job.Id}/fail", new { job.LeaseToken, errorCode, transient }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static Uri ValidateServerUrl(string value)
    {
        if (!Uri.TryCreate(value.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new InvalidOperationException("Use an HTTPS server URL (HTTP is accepted only for localhost development).");
        return uri;
    }

    private static string? ResolveLanIpv4Address()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up &&
                              network.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
            .SelectMany(network => network.GetIPProperties().UnicastAddresses)
            .Select(address => address.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            .OrderByDescending(IsPrivateAddress)
            .ThenBy(address => address.ToString(), StringComparer.Ordinal)
            .ToList();
        return addresses.FirstOrDefault()?.ToString();
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 || bytes[0] == 172 && bytes[1] is >= 16 and <= 31;
    }

    public void Dispose() => _client.Dispose();
}
