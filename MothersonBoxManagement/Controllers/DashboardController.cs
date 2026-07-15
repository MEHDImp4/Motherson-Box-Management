using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Models;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private const int DefaultOpenBoxesPageSize = 8;
    private readonly IBoxService _boxService;
    private readonly IBarcodeService _barcodeService;
    private readonly IBoxTemplateService _boxTemplateService;

    public DashboardController(IBoxService boxService, IBarcodeService barcodeService, IBoxTemplateService boxTemplateService)
    {
        _boxService = boxService;
        _barcodeService = barcodeService;
        _boxTemplateService = boxTemplateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = DefaultOpenBoxesPageSize, CancellationToken cancellationToken = default)
    {
        var fullName = User.FindFirst("FullName")?.Value;
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var openPaged = await _boxService.GetOpenBoxesPagedAsync(page, pageSize, cancellationToken);
        var createdBoxes = await _boxService.GetCreatedBoxesPagedAsync(1, 100, cancellationToken);

        var model = new HomeViewModel
        {
            Matricule = User.FindFirst("Matricule")?.Value,
            FullName = fullName,
            Role = role,
            OpenBoxes = openPaged.Items,
            CreatedBoxes = createdBoxes.Items,
            CurrentOpenBoxesPage = openPaged.Page,
            OpenBoxesPageSize = openPaged.PageSize,
            TotalOpenBoxesCount = openPaged.TotalCount,
            OpenBoxesTotalPages = openPaged.TotalPages
        };
        model.Error = TempData["Error"] as string;

        return View(model);
    }

    [HttpGet]
    [Route("Dashboard/Templates")]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken = default)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!CanCreateBoxes(role))
        {
            return Forbid();
        }

        var model = new TemplateSelectionViewModel
        {
            Role = role,
            Templates = (await _boxTemplateService.GetActiveTemplatesAsync(cancellationToken)).AsReadOnly()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HomeViewModel postModel, CancellationToken cancellationToken)
    {
        var barcode = postModel.Barcode?.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            var emptyModel = await BuildModelAsync("Please enter a barcode.", null, cancellationToken);
            emptyModel.Barcode = postModel.Barcode;
            return View(emptyModel);
        }

        if (!_barcodeService.IsBoxBarcode(barcode))
        {
            var warnModel = await BuildModelAsync(null, "This is a package barcode, not a box barcode. Use the preparation screen to scan packages.", cancellationToken);
            warnModel.Barcode = postModel.Barcode;
            return View(warnModel);
        }

        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            var notFoundModel = await BuildModelAsync("No box was found with this barcode. This code does not match a valid box.", null, cancellationToken);
            notFoundModel.Barcode = postModel.Barcode;
            return View(notFoundModel);
        }

        if (box.Status == BoxStatus.Blocked)
        {
            var blockedModel = await BuildModelAsync(null, "This box is quarantined (Blocked). A supervisor must unblock it before operations can resume.", cancellationToken);
            blockedModel.Barcode = postModel.Barcode;
            blockedModel.ScannedBox = box;
            return View(blockedModel);
        }

        return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
    }

    private async Task<HomeViewModel> BuildModelAsync(string? error, string? warning, CancellationToken cancellationToken)
    {
        var fullName = User.FindFirst("FullName")?.Value;
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var openPaged = await _boxService.GetOpenBoxesPagedAsync(1, DefaultOpenBoxesPageSize, cancellationToken);
        var createdBoxes = await _boxService.GetCreatedBoxesPagedAsync(1, 100, cancellationToken);
        return new HomeViewModel
        {
            Matricule = User.FindFirst("Matricule")?.Value,
            FullName = fullName,
            Role = role,
            OpenBoxes = openPaged.Items,
            CreatedBoxes = createdBoxes.Items,
            CurrentOpenBoxesPage = openPaged.Page,
            OpenBoxesPageSize = openPaged.PageSize,
            TotalOpenBoxesCount = openPaged.TotalCount,
            OpenBoxesTotalPages = openPaged.TotalPages,
            Error = error,
            Warning = warning
        };
    }

    private static bool CanCreateBoxes(string? role)
    {
        return string.Equals(role, AppRoles.Operator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AppRoles.Supervisor, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AppRoles.SupervisorFr, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, AppRoles.AdminFr, StringComparison.OrdinalIgnoreCase);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };

        if (statusCode.HasValue)
            Response.StatusCode = statusCode.Value;

        return View(model);
    }
}
