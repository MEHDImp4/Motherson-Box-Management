using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Models;
using MothersonBoxManagement.Services;

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

    public async Task<IActionResult> Index()
    {
        ViewBag.Matricule = User.FindFirst("Matricule")?.Value;
        ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;
        ViewBag.OpenBoxes = await _boxService.GetOpenBoxesAsync();
        ViewBag.Error = TempData["Error"] as string;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            ViewBag.Error = "Veuillez entrer un code-barres.";
            ViewBag.OpenBoxes = await _boxService.GetOpenBoxesAsync();
            return View();
        }

        var box = await _boxService.GetBoxByBarcodeAsync(barcode);
        if (box is null)
        {
            ViewBag.Error = "Aucune box trouvée avec ce code-barres.";
            ViewBag.OpenBoxes = await _boxService.GetOpenBoxesAsync();
            ViewBag.Matricule = User.FindFirst("Matricule")?.Value;
            ViewBag.Role = User.FindFirst(ClaimTypes.Role)?.Value;
            return View();
        }

        return RedirectToAction("Details", "Box", new { id = box.Id });
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
