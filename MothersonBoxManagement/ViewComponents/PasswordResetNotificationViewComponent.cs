using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.ViewComponents;

public sealed class PasswordResetNotificationViewComponent : ViewComponent
{
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordResetNotificationViewComponent(IPasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await _passwordRecoveryService.CountPendingAsync(HttpContext.RequestAborted));
}
