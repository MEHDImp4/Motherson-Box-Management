using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.ViewModels;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class BoxController : Controller
{
    private readonly IBoxService _boxService;
    private readonly IPackageScanService _packageScanService;
    private readonly IBarcodeService _barcodeService;
    private readonly IWorkstationResolver _workstationResolver;

    public BoxController(
        IBoxService boxService,
        IPackageScanService packageScanService,
        IBarcodeService barcodeService,
        IWorkstationResolver workstationResolver)
    {
        _boxService = boxService;
        _packageScanService = packageScanService;
        _barcodeService = barcodeService;
        _workstationResolver = workstationResolver;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity is not authenticated.");
        return int.Parse(claim.Value);
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

        if (User.IsInRole(AppRoles.Supervisor) || User.IsInRole(AppRoles.AdminFr) || User.IsInRole(AppRoles.SupervisorFr) || User.IsInRole(AppRoles.Administrator))
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
        var workstationName = _workstationResolver.Resolve();
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
        var workstationName = _workstationResolver.Resolve();
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
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("scan")]
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
        var workstationName = _workstationResolver.Resolve();
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
                scannedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
                scannedBy = User.FindFirst(ClaimTypes.Name)?.Value
            } : null
        });
    }
}
