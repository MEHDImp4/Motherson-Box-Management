using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Controllers;

[Authorize]
[Authorize(Roles = $"{AppRoles.SupervisorFr},{AppRoles.AdminFr},{AppRoles.Supervisor},{AppRoles.Administrator}")]
public class BoxOperationsController : Controller
{
    private readonly IBoxService _boxService;
    private readonly IWorkstationResolver _workstationResolver;

    public BoxOperationsController(IBoxService boxService, IWorkstationResolver workstationResolver)
    {
        _boxService = boxService;
        _workstationResolver = workstationResolver;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity is not authenticated.");
        return int.Parse(claim.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/CancelBox")]
    public async Task<IActionResult> CancelBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.CancelBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box cancelled successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while cancelling the box. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ForceCloseBox")]
    public async Task<IActionResult> ForceCloseBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.ForceCloseBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box closed successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while force closing the box. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ModifyExpectedQuantity")]
    public async Task<IActionResult> ModifyExpectedQuantity(int boxId, string boxBarcode, int expectedQuantity, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        if (expectedQuantity <= 0)
        {
            TempData["Error"] = "Expected quantity must be greater than 0.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.UpdateExpectedQuantityAsync(boxId, expectedQuantity, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Expected quantity updated successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while updating the quantity. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/BlockBox")]
    public async Task<IActionResult> BlockBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.BlockBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box blocked successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while blocking the box. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/UnblockBox")]
    public async Task<IActionResult> UnblockBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.UnblockBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box unblocked successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while unblocking the box. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/ArchiveBox")]
    public async Task<IActionResult> ArchiveBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.ArchiveBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box archived successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while archiving the box. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/BlockPackage")]
    public async Task<IActionResult> BlockPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.BlockPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package blocked successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while blocking the package. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/UnblockPackage")]
    public async Task<IActionResult> UnblockPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.UnblockPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package unblocked successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while unblocking the package. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/TransferPackage")]
    public async Task<IActionResult> TransferPackage(int packageId, string boxBarcode, int destinationBoxId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        if (destinationBoxId <= 0)
        {
            TempData["Error"] = "The destination box is invalid.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.TransferPackageAsync(packageId, destinationBoxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package transferred successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while transferring the package. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/RetraitPackage")]
    public async Task<IActionResult> RetraitPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.RetraitPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package removed successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while removing the package. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/DisassociatePackage")]
    public async Task<IActionResult> DisassociatePackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = _workstationResolver.Resolve();
            var box = await _boxService.DisassociatePackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package disassociated successfully.";
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while disassociating the package. Please try again.";
            return RedirectToAction("Details", "Box", new { barcode = boxBarcode });
        }
    }
}
