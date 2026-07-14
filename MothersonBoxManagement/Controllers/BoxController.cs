using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Models;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class BoxController : Controller
{
    private readonly IBoxService _boxService;
    private readonly IBoxTemplateService _boxTemplateService;
    private readonly IPackageScanService _packageScanService;
    private readonly IBarcodeService _barcodeService;
    private readonly IWorkstationResolver _workstationResolver;
    private readonly IQrCodeService _qrCodeService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BoxController> _logger;

    public BoxController(
        IBoxService boxService,
        IBoxTemplateService boxTemplateService,
        IPackageScanService packageScanService,
        IBarcodeService barcodeService,
        IWorkstationResolver workstationResolver,
        IQrCodeService qrCodeService,
        IUserService userService,
        ApplicationDbContext context,
        ILogger<BoxController> logger)
    {
        _boxService = boxService;
        _boxTemplateService = boxTemplateService;
        _packageScanService = packageScanService;
        _barcodeService = barcodeService;
        _workstationResolver = workstationResolver;
        _qrCodeService = qrCodeService;
        _userService = userService;
        _context = context;
        _logger = logger;
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
        var users = await _userService.GetActiveUsersAsync(cancellationToken);

        ViewBag.Users = users;
        ViewBag.Filter = filter;

        return View(boxes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Box/CreateFromTemplate")]
    public async Task<IActionResult> CreateFromTemplate([FromForm] int templateId, [FromForm] string? workstationName, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var resolvedWorkstation = _workstationResolver.Resolve(workstationName);

        try
        {
            var box = await _boxTemplateService.CreateBoxFromTemplateAsync(templateId, userId, resolvedWorkstation, cancellationToken);
            var printQueued = await _context.BoxPrintJobs.AnyAsync(job => job.BoxId == box.Id, cancellationToken);

            if (User.IsInRole(AppRoles.Operator) && !printQueued)
            {
                return RedirectToAction("PrintClient", "Print", new
                {
                    barcode = box.BarcodeValue,
                    autoPrint = true,
                    returnUrl = Url.Action(nameof(Details), new { barcode = box.BarcodeValue })
                });
            }

            TempData["ScanSuccess"] = printQueued
                ? $"Box {box.BoxNumber} created; label queued for the workstation printer."
                : $"Box {box.BoxNumber} created and opened.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Dashboard");
        }
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public async Task<IActionResult> Open(int boxId, string boxBarcode, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var workstationName = _workstationResolver.Resolve();

        try
        {
            var box = await _boxService.OpenBoxAsync(boxId, userId, workstationName, cancellationToken);

            TempData["ScanSuccess"] = $"Box {box.BoxNumber} opened successfully.";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }

    [HttpPost]
    [Route("Box/AutoScanPackage")]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("scan")]
    public async Task<IActionResult> AutoScanPackage([FromForm] string packageBarcode, [FromForm] string? workstationName, [FromForm] string? requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(packageBarcode))
            return Json(new { success = false, noMatch = false, message = "No package provided." });

        packageBarcode = packageBarcode.Trim();

        if (!_barcodeService.IsPackageBarcode(packageBarcode))
            return Json(new { success = false, noMatch = false, message = "Invalid package code." });

        var existingPackage = await _context.BoxPackages
            .FirstOrDefaultAsync(bp => bp.PackageBarcode == packageBarcode, cancellationToken);

        if (existingPackage is not null)
            return Json(new { success = false, noMatch = false, message = "This package is already associated with a box." });

        var template = await _boxTemplateService.FindTemplateByPackageBarcodeAsync(packageBarcode, cancellationToken);

        if (template is null)
            return Json(new { success = false, noMatch = true, message = "No template matches this barcode prefix." });

        var userId = GetUserId();
        var resolvedWorkstation = _workstationResolver.Resolve(workstationName);

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        BoxDetailsDto? createdBox = null;
        try
        {
            createdBox = await _boxTemplateService.CreateBoxFromTemplateAsync(template.Id, userId, resolvedWorkstation, cancellationToken);

            var scanResult = await _packageScanService.ScanPackageAsync(createdBox.Id, packageBarcode, userId, resolvedWorkstation, cancellationToken, requestId);

            if (!scanResult.Success)
            {
                await RollBackAutoCreatedBoxAsync(createdBox.Id, transaction is not null, cancellationToken);
                return Json(new { success = false, noMatch = false, message = scanResult.Message });
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            var printQueued = await _context.BoxPrintJobs.AnyAsync(job => job.BoxId == createdBox.Id, cancellationToken);

            return Json(new
            {
                success = true,
                noMatch = false,
                message = $"Box {createdBox.BoxNumber} auto-created from template \"{template.Name}\". {scanResult.Message}",
                currentQuantity = scanResult.Box?.CurrentQuantity,
                expectedQuantity = scanResult.Box?.ExpectedQuantity,
                status = scanResult.Box?.Status.ToString(),
                boxNumber = createdBox.BoxNumber,
                printQueued
            });
        }
        catch (Exception ex)
        {
            if (createdBox is not null)
                await RollBackAutoCreatedBoxAsync(createdBox.Id, transaction is not null, cancellationToken);
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Auto-creation failed. CorrelationId={CorrelationId}", correlationId);
            return Json(new
            {
                success = false,
                noMatch = false,
                message = $"Auto-creation failed. Contact support with reference {correlationId}."
            });
        }
    }

    private async Task RollBackAutoCreatedBoxAsync(int boxId, bool hasRelationalTransaction, CancellationToken cancellationToken)
    {
        if (hasRelationalTransaction)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            return;
        }

        _context.BoxPrintJobs.RemoveRange(_context.BoxPrintJobs.Where(job => job.BoxId == boxId));
        _context.BoxAuditLogs.RemoveRange(_context.BoxAuditLogs.Where(log => log.BoxId == boxId));
        _context.BoxPackages.RemoveRange(_context.BoxPackages.Where(package => package.BoxId == boxId));
        _context.Boxes.RemoveRange(_context.Boxes.Where(box => box.Id == boxId));
        await _context.SaveChangesAsync(cancellationToken);
    }

    [HttpPost]
    [Route("Box/AssociatePackage")]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("scan")]
    public async Task<IActionResult> AssociatePackage([FromForm] string packageBarcode, [FromForm] string boxBarcode, [FromForm] string? workstationName, [FromForm] string? requestId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(packageBarcode))
            return Json(new { success = false, message = "No package provided." });

        if (string.IsNullOrWhiteSpace(boxBarcode))
            return Json(new { success = false, message = "No box provided." });

        packageBarcode = packageBarcode.Trim();
        boxBarcode = boxBarcode.Trim();

        if (!_barcodeService.IsPackageBarcode(packageBarcode))
            return Json(new { success = false, message = "Invalid package code." });

        if (!_barcodeService.IsBoxBarcode(boxBarcode))
            return Json(new { success = false, message = "Invalid box code. Expected format: BOX-..." });

        var box = await _boxService.GetBoxByBarcodeAsync(boxBarcode, cancellationToken);
        if (box is null)
            return Json(new { success = false, message = "No box found with code " + boxBarcode + "." });

        var userId = GetUserId();
        var resolvedWorkstation = _workstationResolver.Resolve(workstationName);
        var result = await _packageScanService.ScanPackageAsync(box.Id, packageBarcode, userId, resolvedWorkstation, cancellationToken, requestId);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            currentQuantity = result.Box?.CurrentQuantity,
            expectedQuantity = result.Box?.ExpectedQuantity,
            status = result.Box?.Status.ToString(),
            boxNumber = result.Box?.BoxNumber
        });
    }

    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var barcodeConfig = await _context.BarcodeConfigurations
            .FirstOrDefaultAsync(bc => bc.Id == 1, cancellationToken)
            ?? new Entities.BarcodeConfiguration { Id = 1 };

        ViewBag.BarcodeConfig = barcodeConfig;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdministratorOnly)]
    public async Task<IActionResult> SaveBarcodeConfig(
        string boxPrefix,
        string boxDatePattern,
        int boxRandomLength,
        string? packagePrefix,
        int packageMinLength,
        CancellationToken cancellationToken)
    {
        var config = await _context.BarcodeConfigurations
            .FirstOrDefaultAsync(bc => bc.Id == 1, cancellationToken);

        if (config is null)
        {
            config = new Entities.BarcodeConfiguration { Id = 1, CreatedAt = DateTime.UtcNow };
            _context.BarcodeConfigurations.Add(config);
        }

        config.BoxPrefix = string.IsNullOrWhiteSpace(boxPrefix) ? "BOX-" : boxPrefix.Trim();
        config.BoxDatePattern = string.IsNullOrWhiteSpace(boxDatePattern) ? "yyyyMMdd" : boxDatePattern.Trim();
        config.BoxRandomLength = Math.Clamp(boxRandomLength, 2, 16);
        config.PackagePrefix = string.IsNullOrWhiteSpace(packagePrefix) ? null : packagePrefix.Trim();
        config.PackageMinLength = Math.Clamp(packageMinLength, 1, 50);
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Barcode configuration saved successfully.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    public IActionResult SearchPackage()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchPackage(string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            ViewBag.Error = "Please enter a package barcode.";
            return View();
        }

        barcode = barcode.Trim();

        if (_barcodeService.IsBoxBarcode(barcode))
        {
            ViewBag.Error = "This code is a box barcode, not a package barcode. Use the box search from the dashboard.";
            return View();
        }

        var box = await _boxService.FindBoxByPackageBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            ViewBag.Error = $"No box contains the package \"{barcode}\". This package is not recognized or has not yet been associated with a box.";
            return View();
        }

        ViewBag.Found = true;
        ViewBag.SearchedBarcode = barcode;
        return View(box);
    }
}
