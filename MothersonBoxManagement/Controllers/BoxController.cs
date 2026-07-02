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

    public BoxController(IBoxService boxService)
    {
        _boxService = boxService;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateBoxViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBoxViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var dto = new CreateBoxDto
        {
            Type = model.Type,
            Height = model.Height,
            Width = model.Width,
            Depth = model.Depth,
            ExpectedQuantity = model.ExpectedQuantity
        };

        var box = await _boxService.CreateBoxAsync(dto, userId, cancellationToken);
        return RedirectToAction("Details", new { barcode = box.BarcodeValue });
    }

    [HttpGet]
    [Route("Box/Details/{barcode}")]
    public async Task<IActionResult> Details(string barcode, CancellationToken cancellationToken)
    {
        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();

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

        return View(box);
    }

    [HttpGet]
    public async Task<IActionResult> ByBarcode(string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return RedirectToAction("Index", "Home");

        var box = await _boxService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
        {
            TempData["Error"] = "Aucune box trouvée avec ce code-barres.";
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Details", new { barcode = box.BarcodeValue });
    }

    [HttpPost]
    public async Task<IActionResult> Scan(int boxId, string boxBarcode, string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            TempData["ScanError"] = "Aucun code-barres fourni.";
            return RedirectToAction("Prepare", new { barcode = boxBarcode });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _boxService.ScanPackageAsync(boxId, barcode, userId, cancellationToken);

        if (result.Success)
            TempData["ScanSuccess"] = result.Message;
        else
            TempData["ScanError"] = result.Message;

        return RedirectToAction("Prepare", new { barcode = boxBarcode });
    }
}
