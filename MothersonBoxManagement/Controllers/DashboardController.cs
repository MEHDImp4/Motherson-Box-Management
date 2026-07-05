using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Models;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.ViewModels;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;
    private readonly IBoxService _boxService;
    private readonly IBarcodeService _barcodeService;

    public DashboardController(ILogger<DashboardController> logger, IBoxService boxService, IBarcodeService barcodeService)
    {
        _logger = logger;
        _boxService = boxService;
        _barcodeService = barcodeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeViewModel
        {
            Matricule = User.FindFirst("Matricule")?.Value,
            Role = User.FindFirst(ClaimTypes.Role)?.Value,
            OpenBoxes = await _boxService.GetOpenBoxesAsync(cancellationToken),
            Error = TempData["Error"] as string
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HomeViewModel postModel, CancellationToken cancellationToken)
    {
        var matricule = User.FindFirst("Matricule")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var openBoxes = await _boxService.GetOpenBoxesAsync(cancellationToken);

        var model = new HomeViewModel
        {
            Matricule = matricule,
            Role = role,
            OpenBoxes = openBoxes,
            Barcode = postModel.Barcode
        };

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
            return RedirectToAction("Prepare", "Box", new { barcode = box.BarcodeValue });
        }
        else
        {
            return RedirectToAction("Details", "Box", new { barcode = box.BarcodeValue });
        }
    }

    public IActionResult Privacy()
    {
        return View();
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
