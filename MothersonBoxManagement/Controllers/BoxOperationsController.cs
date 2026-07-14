using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Controllers;

[Authorize]
[Authorize(Roles = $"{AppRoles.SupervisorFr},{AppRoles.AdminFr},{AppRoles.Supervisor},{AppRoles.Administrator}")]
public class BoxOperationsController : Controller
{
    private readonly IBoxService _boxService;
    private readonly IWorkstationResolver _workstationResolver;
    private readonly ILogger<BoxOperationsController> _logger;

    public BoxOperationsController(IBoxService boxService, IWorkstationResolver workstationResolver, ILogger<BoxOperationsController> logger)
    {
        _boxService = boxService;
        _workstationResolver = workstationResolver;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity is not authenticated.");
        return int.Parse(claim.Value);
    }

    private string ResolveWorkstation(string? workstationName)
    {
        return _workstationResolver.Resolve(workstationName);
    }

    private async Task<IActionResult> ExecuteBoxOperationAsync(
        string boxBarcode,
        string errorLogMessage,
        Func<int, string, CancellationToken, Task<BoxDetailsDto>> operation,
        string successMessage,
        string? workstationName,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var resolvedWorkstationName = ResolveWorkstation(workstationName);
            var box = await operation(userId, resolvedWorkstationName, cancellationToken);
            TempData["ScanSuccess"] = successMessage;
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ErrorMessage}", errorLogMessage);
            TempData["Error"] = "An error occurred. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    private IActionResult RequireReason(string boxBarcode)
    {
        TempData["Error"] = "A reason is required.";
        return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/CancelBox")]
    public Task<IActionResult> CancelBox(int boxId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error cancelling box {boxId}",
            (userId, ws, ct) => _boxService.CancelBoxAsync(boxId, reason, userId, ws, ct),
            "Box cancelled successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ForceCloseBox")]
    public Task<IActionResult> ForceCloseBox(int boxId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error force closing box {boxId}",
            (userId, ws, ct) => _boxService.ForceCloseBoxAsync(boxId, reason, userId, ws, ct),
            "Box closed successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ModifyExpectedQuantity")]
    public Task<IActionResult> ModifyExpectedQuantity(int boxId, string boxBarcode, int expectedQuantity, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        if (expectedQuantity <= 0)
        {
            TempData["Error"] = "Expected quantity must be greater than 0.";
            return Task.FromResult<IActionResult>(RedirectToAction("Details", "Box", new { barcode = boxBarcode }));
        }

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error updating quantity for box {boxId}",
            (userId, ws, ct) => _boxService.UpdateExpectedQuantityAsync(boxId, expectedQuantity, reason, userId, ws, ct),
            "Expected quantity updated successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/BlockBox")]
    public Task<IActionResult> BlockBox(int boxId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error blocking box {boxId}",
            (userId, ws, ct) => _boxService.BlockBoxAsync(boxId, reason, userId, ws, ct),
            "Box blocked successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/UnblockBox")]
    public Task<IActionResult> UnblockBox(int boxId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error unblocking box {boxId}",
            (userId, ws, ct) => _boxService.UnblockBoxAsync(boxId, reason, userId, ws, ct),
            "Box unblocked successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ArchiveBox")]
    public Task<IActionResult> ArchiveBox(int boxId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error archiving box {boxId}",
            (userId, ws, ct) => _boxService.ArchiveBoxAsync(boxId, reason, userId, ws, ct),
            "Box archived successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/BlockPackage")]
    public Task<IActionResult> BlockPackage(int packageId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error blocking package {packageId}",
            (userId, ws, ct) => _boxService.BlockPackageAsync(packageId, reason, userId, ws, ct),
            "Package blocked successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/UnblockPackage")]
    public Task<IActionResult> UnblockPackage(int packageId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error unblocking package {packageId}",
            (userId, ws, ct) => _boxService.UnblockPackageAsync(packageId, reason, userId, ws, ct),
            "Package unblocked successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/TransferPackage")]
    public Task<IActionResult> TransferPackage(int packageId, string boxBarcode, int destinationBoxId, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        if (destinationBoxId <= 0)
        {
            TempData["Error"] = "The destination box is invalid.";
            return Task.FromResult<IActionResult>(RedirectToAction("Details", "Box", new { barcode = boxBarcode }));
        }

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error transferring package {packageId}",
            (userId, ws, ct) => _boxService.TransferPackageAsync(packageId, destinationBoxId, reason, userId, ws, ct),
            "Package transferred successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/RetraitPackage")]
    public Task<IActionResult> RetraitPackage(int packageId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error removing package {packageId}",
            (userId, ws, ct) => _boxService.RetraitPackageAsync(packageId, reason, userId, ws, ct),
            "Package removed successfully.", workstationName, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/DisassociatePackage")]
    public Task<IActionResult> DisassociatePackage(int packageId, string boxBarcode, string reason, string? workstationName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Task.FromResult(RequireReason(boxBarcode));

        return ExecuteBoxOperationAsync(boxBarcode,
            $"Error disassociating package {packageId}",
            (userId, ws, ct) => _boxService.DisassociatePackageAsync(packageId, reason, userId, ws, ct),
            "Package disassociated successfully.", workstationName, ct);
    }
}
