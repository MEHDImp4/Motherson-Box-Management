using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Controllers;

[Authorize(Roles = $"{AppRoles.SupervisorFr},{AppRoles.AdminFr},{AppRoles.Supervisor},{AppRoles.Administrator}")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? boxId,
        string? actionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new AuditFilterDto
        {
            BoxId = boxId,
            ActionType = actionType,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var model = await _auditService.GetAuditLogsAsync(filter, cancellationToken);
        return View(model);
    }
}
