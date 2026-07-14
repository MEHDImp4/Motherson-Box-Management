using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;

namespace MothersonBoxManagement.Controllers;

[ApiController]
[Route("api/print-agent")]
[EnableRateLimiting("print-agent")]
public sealed class PrintAgentApiController : ControllerBase
{
    private readonly IPrintAgentService _service;
    private readonly ILogger<PrintAgentApiController> _logger;

    public PrintAgentApiController(IPrintAgentService service, ILogger<PrintAgentApiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("pair")]
    public async Task<ActionResult<PairAgentResult>> Pair(PairAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.PairAsync(request.Code, request.MachineName, request.AgentVersion, cancellationToken));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            _logger.LogWarning("Print-agent pairing rejected from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            return BadRequest(new { error = "PAIRING_REJECTED" });
        }
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(HeartbeatRequest request, CancellationToken cancellationToken)
    {
        var workstation = await AuthenticateAsync(cancellationToken);
        if (workstation is null)
            return Unauthorized();

        try
        {
            await _service.HeartbeatAsync(workstation.Id,
                new AgentHeartbeat(request.MachineName, request.AgentVersion, request.Printers ?? []),
                cancellationToken);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "INVALID_HEARTBEAT" });
        }
    }

    [HttpGet("jobs/next")]
    public async Task<ActionResult<ClaimedPrintJob>> Next(CancellationToken cancellationToken)
    {
        var workstation = await AuthenticateAsync(cancellationToken);
        if (workstation is null)
            return Unauthorized();
        var job = await _service.ClaimNextAsync(workstation.Id, cancellationToken: cancellationToken);
        return job is null ? NoContent() : Ok(job);
    }

    [HttpPost("jobs/{jobId:int}/complete")]
    public async Task<IActionResult> Complete(int jobId, LeaseRequest request, CancellationToken cancellationToken)
    {
        var workstation = await AuthenticateAsync(cancellationToken);
        if (workstation is null)
            return Unauthorized();
        try
        {
            await _service.CompleteAsync(workstation.Id, jobId, request.LeaseToken, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { error = "INVALID_LEASE" });
        }
    }

    [HttpPost("jobs/{jobId:int}/fail")]
    public async Task<IActionResult> Fail(int jobId, FailJobRequest request, CancellationToken cancellationToken)
    {
        var workstation = await AuthenticateAsync(cancellationToken);
        if (workstation is null)
            return Unauthorized();
        try
        {
            await _service.FailAsync(workstation.Id, jobId, request.LeaseToken, request.ErrorCode, request.Transient, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { error = "INVALID_LEASE" });
        }
    }

    private async Task<PrinterConfiguration?> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var token = authorization["Bearer ".Length..].Trim();
        return await _service.AuthenticateAsync(token, cancellationToken);
    }
}

public sealed record PairAgentRequest(string Code, string MachineName, string AgentVersion);
public sealed record HeartbeatRequest(string MachineName, string AgentVersion, string[]? Printers);
public sealed record LeaseRequest(string LeaseToken);
public sealed record FailJobRequest(string LeaseToken, string ErrorCode, bool Transient);
