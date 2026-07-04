using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.ViewModels;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class BoxController : Controller
{
    private readonly IBoxService _boxService;
    private readonly IPackageScanService _packageScanService;
    private readonly IBarcodeService _barcodeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public BoxController(
        IBoxService boxService,
        IPackageScanService packageScanService,
        IBarcodeService barcodeService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _boxService = boxService;
        _packageScanService = packageScanService;
        _barcodeService = barcodeService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    private string GetWorkstationName()
    {
        string? configured = _configuration["WorkstationName"];
        if (string.IsNullOrWhiteSpace(configured) || configured == "DEV-STATION-01" || configured == "DEFAULT-STATION")
        {
            return Environment.MachineName;
        }
        return configured;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Index(BoxSearchFilterDto filter, CancellationToken cancellationToken)
    {
        var boxes = await _boxService.SearchBoxesAsync(filter, cancellationToken);
        var users = await _boxService.GetUsersAsync(cancellationToken);

        ViewBag.Users = users;
        ViewBag.Filter = filter;

        return View(boxes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateBoxViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBoxViewModel model, CancellationToken cancellationToken)
    {
        if (model.Height % 1 != 0)
            ModelState.AddModelError(nameof(model.Height), "Height must be a positive whole number.");
        if (model.Width % 1 != 0)
            ModelState.AddModelError(nameof(model.Width), "Width must be a positive whole number.");
        if (model.Depth % 1 != 0)
            ModelState.AddModelError(nameof(model.Depth), "Depth must be a positive whole number.");

        if (!ModelState.IsValid)
            return View(model);

        var userId = GetUserId();

        var dto = new CreateBoxDto
        {
            Type = model.Type,
            Height = model.Height,
            Width = model.Width,
            Depth = model.Depth,
            ExpectedQuantity = model.ExpectedQuantity
        };

        var box = await _boxService.CreateBoxAsync(dto, userId, cancellationToken);
        return RedirectToAction("Prepare", new { barcode = box.BarcodeValue });
    }

    [HttpGet]
    [Route("Box/Details/{barcode}")]
    public async Task<IActionResult> Details(string barcode, CancellationToken cancellationToken)
    {
        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();

        if (User.IsInRole("Superviseur") || User.IsInRole("Admin") || User.IsInRole("Supervisor") || User.IsInRole("Administrator"))
        {
            var openBoxes = await _boxService.GetOpenBoxesAsync(cancellationToken);
            ViewBag.OpenBoxes = openBoxes.Where(b => b.Id != box.Id).ToList();
        }

        return View(box);
    }

    [HttpGet]
    [Route("Box/Prepare/{barcode}")]
    public async Task<IActionResult> Prepare(string barcode, CancellationToken cancellationToken)
    {
        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();

        if (box.Status != BoxStatus.Open)
        {
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }

        var userId = GetUserId();
        var workstationName = GetWorkstationName();
        await _boxService.LogBoxResumedIfNeededAsync(box.Id, userId, workstationName, cancellationToken);

        var vm = new PrepareViewModel { Box = box };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ByBarcode(string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return RedirectToAction("Index", "Dashboard");

        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            TempData["Error"] = "No box was found with this barcode.";
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction("Details", new { barcode = box.BarcodeValue });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan(int boxId, string boxBarcode, string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            TempData["ScanError"] = "No barcode was provided.";
            return RedirectToAction("Prepare", new { barcode = boxBarcode });
        }

        if (barcode.Trim().Length < 3)
        {
            TempData["ScanError"] = "The barcode must contain at least 3 characters.";
            return RedirectToAction("Prepare", new { barcode = boxBarcode });
        }

        var userId = GetUserId();
        var workstationName = GetWorkstationName();
        var result = await _packageScanService.ScanPackageAsync(boxId, barcode, userId, workstationName, cancellationToken);

        if (result.Success)
            TempData["ScanSuccess"] = result.Message;
        else
            TempData["ScanError"] = result.Message;

        return RedirectToAction("Prepare", new { barcode = boxBarcode });
    }

    [HttpPost]
    [Route("Box/ScanAjax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanAjax([FromForm] int boxId, [FromForm] string boxBarcode, [FromForm] string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return Json(new { success = false, message = "No barcode was provided." });
        }

        barcode = barcode.Trim();
        if (barcode.Length < 3)
        {
            return Json(new { success = false, message = "The barcode must contain at least 3 characters." });
        }

        var userId = GetUserId();
        var workstationName = GetWorkstationName();
        var result = await _packageScanService.ScanPackageAsync(boxId, barcode, userId, workstationName, cancellationToken);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            currentQuantity = result.Box?.CurrentQuantity,
            expectedQuantity = result.Box?.ExpectedQuantity,
            status = result.Box?.Status.ToString(),
            package = result.Success ? new
            {
                barcode = barcode,
                scannedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                scannedBy = User.FindFirst(ClaimTypes.Name)?.Value
            } : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> CancelBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.CancelBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box cancelled successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while cancelling: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> ForceCloseBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.ForceCloseBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box closed successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while force closing: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> ModifyExpectedQuantity(int boxId, string boxBarcode, int expectedQuantity, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        if (expectedQuantity <= 0)
        {
            TempData["Error"] = "Expected quantity must be greater than 0.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.UpdateExpectedQuantityAsync(boxId, expectedQuantity, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Expected quantity updated successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while updating the quantity: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> BlockBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.BlockBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box blocked successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while blocking: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> UnblockBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.UnblockBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box unblocked successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while unblocking: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> ArchiveBox(int boxId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.ArchiveBoxAsync(boxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Box archived successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while archiving: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> BlockPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.BlockPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package blocked successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while blocking the package: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> UnblockPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.UnblockPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package unblocked successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while unblocking the package: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> TransferPackage(int packageId, string boxBarcode, int destinationBoxId, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        if (destinationBoxId <= 0)
        {
            TempData["Error"] = "The destination box is invalid.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.TransferPackageAsync(packageId, destinationBoxId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package transferred successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while transferring the package: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> RetraitPackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.RetraitPackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package removed successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while removing the package: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Superviseur,Admin,Supervisor,Administrator")]
    public async Task<IActionResult> DisassociatePackage(int packageId, string boxBarcode, string reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = GetUserId();
            var workstationName = GetWorkstationName();
            var box = await _boxService.DisassociatePackageAsync(packageId, reason, userId, workstationName, cancellationToken);
            TempData["ScanSuccess"] = "Package disassociated successfully.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Error while disassociating the package: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }
}
