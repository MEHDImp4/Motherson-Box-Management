using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Printing;

public interface IPrintAgentService
{
    Task<string> CreatePairingCodeAsync(int workstationId, CancellationToken cancellationToken = default);
    Task<PairAgentResult> PairAsync(string code, string machineName, string agentVersion, CancellationToken cancellationToken = default);
    Task<PrinterConfiguration?> AuthenticateAsync(string token, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(int workstationId, AgentHeartbeat heartbeat, CancellationToken cancellationToken = default);
    Task<BoxPrintJob> QueueAsync(int boxId, int requestedByUserId, int workstationId, string? reprintReason = null, CancellationToken cancellationToken = default);
    Task<ClaimedPrintJob?> ClaimNextAsync(int workstationId, bool ignoreRetryDelay = false, CancellationToken cancellationToken = default);
    Task CompleteAsync(int workstationId, int jobId, string leaseToken, CancellationToken cancellationToken = default);
    Task FailAsync(int workstationId, int jobId, string leaseToken, string errorCode, bool transient, CancellationToken cancellationToken = default);
}
