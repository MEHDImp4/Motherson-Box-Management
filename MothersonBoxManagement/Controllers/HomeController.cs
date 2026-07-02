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
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IBoxService _boxService;

    public HomeController(ILogger<HomeController> logger, IBoxService boxService)
    {
        _logger = logger;
        _boxService = boxService;
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
            model.Error = "Veuillez entrer un code-barres.";
            return View(model);
        }

        if (!barcode.StartsWith("BOX-", StringComparison.OrdinalIgnoreCase))
        {
            model.Warning = "This is a package barcode, not a box barcode. Use the preparation screen to scan packages.";
            return View(model);
        }

        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            model.Error = "No box found with this barcode.";
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
