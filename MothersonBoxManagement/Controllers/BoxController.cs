using System.Security.Claims;
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
    public async Task<IActionResult> Create(CreateBoxViewModel model)
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

        var box = await _boxService.CreateBoxAsync(dto, userId);
        return RedirectToAction("Details", new { id = box.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var box = await _boxService.GetBoxByIdAsync(id);
        if (box is null)
            return NotFound();

        return View(box);
    }

    [HttpGet]
    public async Task<IActionResult> ByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return RedirectToAction("Index", "Home");

        var box = await _boxService.GetBoxByBarcodeAsync(barcode);
        if (box is null)
        {
            TempData["Error"] = "Aucune box trouvée avec ce code-barres.";
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Details", new { id = box.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Scan(int boxId, string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            TempData["ScanError"] = "Aucun code-barres fourni.";
            return RedirectToAction("Details", new { id = boxId });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _boxService.ScanPackageAsync(boxId, barcode, userId);

        if (result.Success)
            TempData["ScanSuccess"] = result.Message;
        else
            TempData["ScanError"] = result.Message;

        return RedirectToAction("Details", new { id = boxId });
    }
}
