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
    [ValidateAntiForgeryToken]
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

        var vm = new PrepareViewModel { Box = box };
        return View(vm);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan(int boxId, string boxBarcode, string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            TempData["ScanError"] = "Aucun code-barres fourni.";
            return RedirectToAction("Prepare", new { barcode = boxBarcode });
        }

        if (barcode.Trim().Length < 3)
        {
            TempData["ScanError"] = "Le code-barres doit contenir au moins 3 caractères.";
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

    [HttpPost]
    [Route("Box/ScanAjax")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanAjax([FromForm] int boxId, [FromForm] string boxBarcode, [FromForm] string barcode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return Json(new { success = false, message = "Aucun code-barres fourni." });
        }

        barcode = barcode.Trim();
        if (barcode.Length < 3)
        {
            return Json(new { success = false, message = "Le code-barres doit contenir au moins 3 caractères." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _boxService.ScanPackageAsync(boxId, barcode, userId, cancellationToken);

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
                scannedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.CancelBoxAsync(boxId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Box annulée avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors de l'annulation: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.ForceCloseBoxAsync(boxId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Box fermée avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors de la fermeture forcée: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        if (expectedQuantity <= 0)
        {
            TempData["Error"] = "La quantité attendue doit être supérieure à 0.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.UpdateExpectedQuantityAsync(boxId, expectedQuantity, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Quantité attendue modifiée avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors de la modification de la quantité: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.BlockBoxAsync(boxId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Box bloquée avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du blocage: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.UnblockBoxAsync(boxId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Box débloquée avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du déblocage: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.BlockPackageAsync(packageId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Paquet bloqué avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du blocage du paquet: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.UnblockPackageAsync(packageId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Paquet débloqué avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du déblocage du paquet: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        if (destinationBoxId <= 0)
        {
            TempData["Error"] = "La box de destination est invalide.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.TransferPackageAsync(packageId, destinationBoxId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Paquet transféré avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du transfert du paquet: {ex.Message}";
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
            TempData["Error"] = "La raison est obligatoire.";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var box = await _boxService.RetraitPackageAsync(packageId, reason, userId, cancellationToken);
            TempData["ScanSuccess"] = "Paquet retiré avec succès.";
            return RedirectToAction("Details", new { barcode = box.BarcodeValue });
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = $"Erreur lors du retrait du paquet: {ex.Message}";
            return RedirectToAction("Details", new { barcode = boxBarcode });
        }
    }
}
