using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Models;

namespace MothersonBoxManagement.Controllers;

[Authorize(Roles = AppRoles.Supervisor + "," + AppRoles.SupervisorFr + "," + AppRoles.Administrator + "," + AppRoles.AdminFr)]
public class BoxTemplateController : Controller
{
    private readonly IBoxTemplateService _boxTemplateService;

    public BoxTemplateController(IBoxTemplateService boxTemplateService)
    {
        _boxTemplateService = boxTemplateService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity is not authenticated.");
        return int.Parse(claim.Value);
    }

    [HttpGet]
    [Route("BoxTemplate")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var templates = await _boxTemplateService.GetActiveTemplatesAsync(cancellationToken);
        return View(templates);
    }

    [HttpGet]
    [Route("BoxTemplate/Create")]
    public IActionResult Create()
    {
        return View(new BoxTemplateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("BoxTemplate/Create")]
    public async Task<IActionResult> Create(BoxTemplateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = GetUserId();

        var dto = new CreateBoxTemplateDto
        {
            Name = model.Name,
            Description = model.Description,
            Type = model.Type,
            Height = model.Height,
            Width = model.Width,
            Depth = model.Depth,
            ExpectedQuantity = model.ExpectedQuantity,
            PackagePrefixPattern = model.PackagePrefixPattern
        };

        await _boxTemplateService.CreateTemplateAsync(dto, userId, cancellationToken);
        TempData["ScanSuccess"] = $"Template \"{model.Name}\" created successfully.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Route("BoxTemplate/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var template = await _boxTemplateService.GetTemplateByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound();

        var model = new BoxTemplateViewModel
        {
            Name = template.Name,
            Description = template.Description,
            Type = template.Type,
            Height = template.Height,
            Width = template.Width,
            Depth = template.Depth,
            ExpectedQuantity = template.ExpectedQuantity,
            PackagePrefixPattern = template.PackagePrefixPattern
        };

        ViewBag.TemplateId = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("BoxTemplate/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, BoxTemplateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TemplateId = id;
            return View(model);
        }

        var dto = new CreateBoxTemplateDto
        {
            Name = model.Name,
            Description = model.Description,
            Type = model.Type,
            Height = model.Height,
            Width = model.Width,
            Depth = model.Depth,
            ExpectedQuantity = model.ExpectedQuantity,
            PackagePrefixPattern = model.PackagePrefixPattern
        };

        await _boxTemplateService.UpdateTemplateAsync(id, dto, cancellationToken);
        TempData["ScanSuccess"] = $"Template \"{model.Name}\" updated successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("BoxTemplate/Deactivate/{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _boxTemplateService.DeactivateTemplateAsync(id, cancellationToken);
        TempData["ScanSuccess"] = "Template deactivated successfully.";
        return RedirectToAction("Index");
    }
}
