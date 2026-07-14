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
    private const int OpenBoxesPageSize = 3;
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
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var fullName = User.FindFirst("FullName")?.Value;
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var openBoxes = await _boxService.GetOpenBoxesAsync(cancellationToken);
        var model = BuildHomeViewModel(
            User.FindFirst("Matricule")?.Value,
            fullName,
            role,
            openBoxes,
            await _boxService.GetCreatedBoxesAsync(cancellationToken),
            page);
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
        var matricule = User.FindFirst("Matricule")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var fullName = User.FindFirst("FullName")?.Value;
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = User.Identity?.Name;
        var openBoxes = await _boxService.GetOpenBoxesAsync(cancellationToken);
        var createdBoxes = await _boxService.GetCreatedBoxesAsync(cancellationToken);

        var model = BuildHomeViewModel(matricule, fullName, role, openBoxes, createdBoxes, 1);
        model.Barcode = postModel.Barcode;

        var barcode = postModel.Barcode?.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            model.Error = "Please enter a barcode.";
            return View(model);
        }

        if (!_barcodeService.IsBoxBarcode(barcode))
        {
            model.Warning = "This is a package barcode, not a box barcode. Use the preparation screen to scan packages.";
            return View(model);
        }

        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            model.Error = "No box was found with this barcode. This code does not match a valid box.";
            return View(model);
        }

        if (box.Status == BoxStatus.Open)
            {
                return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
            }
            else if (box.Status == BoxStatus.Created)
            {
                return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
            }
            else if (box.Status == BoxStatus.Blocked)
            {
                model.Warning = "This box is quarantined (Blocked). A supervisor must unblock it before operations can resume.";
                model.ScannedBox = box;
                return View(model);
            }
            else
            {
                return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
            }
    }

    private static HomeViewModel BuildHomeViewModel(
        string? matricule,
        string? fullName,
        string? role,
        IReadOnlyList<BoxListItemDto> openBoxes,
        IReadOnlyList<BoxListItemDto> createdBoxes,
        int requestedPage)
    {
        var totalOpenBoxesCount = openBoxes.Count;
        var totalPages = totalOpenBoxesCount == 0
            ? 1
            : (int)Math.Ceiling(totalOpenBoxesCount / (double)OpenBoxesPageSize);
        var currentPage = Math.Clamp(requestedPage, 1, totalPages);
        var pagedOpenBoxes = openBoxes
            .Skip((currentPage - 1) * OpenBoxesPageSize)
            .Take(OpenBoxesPageSize)
            .ToList();

        return new HomeViewModel
        {
            Matricule = matricule,
            FullName = fullName,
            Role = role,
            OpenBoxes = pagedOpenBoxes,
            CreatedBoxes = createdBoxes,
            CurrentOpenBoxesPage = currentPage,
            OpenBoxesPageSize = OpenBoxesPageSize,
            TotalOpenBoxesCount = totalOpenBoxesCount,
            OpenBoxesTotalPages = totalPages
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
