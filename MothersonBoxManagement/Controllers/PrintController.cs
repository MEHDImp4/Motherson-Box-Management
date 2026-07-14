using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Models;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Printing;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MothersonBoxManagement.Controllers;

[Authorize]
public class PrintController : Controller
{
    private readonly IBoxQueryService _boxQueryService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ApplicationDbContext _db;
    private readonly IPrintAgentService _printAgentService;
    private readonly IWorkstationResolver _workstationResolver;

    public PrintController(
        IBoxQueryService boxQueryService,
        IQrCodeService qrCodeService,
        ApplicationDbContext db,
        IPrintAgentService printAgentService,
        IWorkstationResolver workstationResolver)
    {
        _boxQueryService = boxQueryService;
        _qrCodeService = qrCodeService;
        _db = db;
        _printAgentService = printAgentService;
        _workstationResolver = workstationResolver;
    }

    [HttpGet]
    [Route("Box/Print/{barcode}")]
    [Authorize(Roles = AppRoles.SupervisorOrAdministrator)]
    public async Task<IActionResult> Print(string barcode, CancellationToken cancellationToken)
    {
        var box = await _boxQueryService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();

        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(box.BarcodeValue);
        var vm = new PrintLabelViewModel
        {
            BoxNumber = box.BoxNumber,
            BarcodeValue = box.BarcodeValue,
            QrCodeBase64 = qrCodeBase64
        };

        return View("~/Views/Box/Print.cshtml", vm);
    }

    [HttpGet]
    [Route("Box/PrintClient/{barcode}")]
    public async Task<IActionResult> PrintClient(string barcode, bool autoPrint = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var box = await _boxQueryService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();

        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(box.BarcodeValue);
        var vm = new PrintLabelViewModel
        {
            BoxNumber = box.BoxNumber,
            BarcodeValue = box.BarcodeValue,
            QrCodeBase64 = qrCodeBase64,
            AutoPrint = autoPrint,
            ReturnUrl = returnUrl
        };

        return View("~/Views/Box/Print.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Print/Queue/{barcode}")]
    public async Task<IActionResult> Queue(string barcode, string? workstationName, CancellationToken cancellationToken)
    {
        var box = await _boxQueryService.GetBoxByBarcodeAsync(barcode, cancellationToken);
        if (box is null)
            return NotFound();
        var station = _workstationResolver.Resolve(workstationName);
        var workstation = await _db.PrinterConfigurations.FirstOrDefaultAsync(candidate =>
            candidate.IsActive && candidate.RevokedAt == null &&
            (candidate.Code == station || candidate.PcName == station), cancellationToken);
        if (workstation is null || string.IsNullOrWhiteSpace(workstation.PrinterName))
        {
            TempData["Error"] = "No active print agent is configured for this workstation. Browser printing remains available.";
            return RedirectToAction(nameof(PrintClient), new { barcode });
        }
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _printAgentService.QueueAsync(box.Id, userId, workstation.Id, "Manual reprint", cancellationToken);
        TempData["ScanSuccess"] = "Label queued for the workstation printer.";
        return RedirectToAction("Details", "Box", new { barcode });
    }

}
