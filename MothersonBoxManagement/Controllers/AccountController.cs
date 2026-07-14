using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Models;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Controllers;

public class AccountController : Controller
{
    private readonly IUserAuthenticationService _authenticationService;
    private readonly ILoginLockoutService _lockoutService;
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public AccountController(
        IUserAuthenticationService authenticationService,
        ILoginLockoutService lockoutService,
        IPasswordRecoveryService passwordRecoveryService)
    {
        _authenticationService = authenticationService;
        _lockoutService = lockoutService;
        _passwordRecoveryService = passwordRecoveryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _lockoutService.IsLockedOutAsync(model.Matricule, cancellationToken))
        {
            var remaining = await _lockoutService.GetRemainingAttemptsAsync(model.Matricule, cancellationToken);
            ModelState.AddModelError(string.Empty, "Account temporarily locked due to too many failed attempts. Please try again later.");
            return View(model);
        }

        var user = await _authenticationService.ValidateCredentialsAsync(model.Matricule, model.Password, cancellationToken);

        if (user is null)
        {
            await _lockoutService.RecordFailedAttemptAsync(model.Matricule, cancellationToken);
            var remaining = await _lockoutService.GetRemainingAttemptsAsync(model.Matricule, cancellationToken);
            if (remaining > 0)
                ModelState.AddModelError(string.Empty, $"Incorrect matricule or password. {remaining} attempt(s) remaining before lockout.");
            else
                ModelState.AddModelError(string.Empty, "Account temporarily locked due to too many failed attempts. Please try again later.");
            return View(model);
        }

        await _lockoutService.ResetAttemptsAsync(model.Matricule, cancellationToken);

        await SignInAsync(user);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RequestPasswordReset(bool submitted = false)
    {
        ViewBag.Submitted = submitted;
        return View(new PasswordResetRequestViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> RequestPasswordReset(PasswordResetRequestViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _passwordRecoveryService.RequestAsync(
            model.Matricule,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return RedirectToAction(nameof(RequestPasswordReset), new { submitted = true });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RecoveryLogin() => View(new RecoveryLoginViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> RecoveryLogin(RecoveryLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var recovery = await _passwordRecoveryService.BeginRecoveryAsync(model.Matricule, cancellationToken);
        if (recovery is null)
        {
            ModelState.AddModelError(string.Empty, "Password reset is not approved or has expired.");
            return View(model);
        }

        await SignInAsync(recovery.Value.User, recovery.Value.Request);
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        if (!HasRecoverySession(out _, out _))
            return RedirectToAction("Index", "Dashboard");
        return View(new ForcedPasswordChangeViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ForcedPasswordChangeViewModel model, CancellationToken cancellationToken)
    {
        if (!HasRecoverySession(out var userId, out var requestId))
            return RedirectToAction("Index", "Dashboard");
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var user = await _passwordRecoveryService.CompleteRecoveryAsync(requestId, userId, model.NewPassword, cancellationToken);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInAsync(user);
            TempData["Success"] = "Password changed successfully.";
            return Redirect("/Dashboard");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private bool HasRecoverySession(out int userId, out int requestId)
    {
        var hasUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        var hasRequestId = int.TryParse(User.FindFirstValue("PasswordResetRequestId"), out requestId);
        return hasUserId && hasRequestId && User.HasClaim("MustChangePassword", "true");
    }

    private async Task SignInAsync(User user, PasswordResetRequest? recoveryRequest = null)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Matricule : user.FullName;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, user.Role),
            new("FullName", displayName),
            new("Matricule", user.Matricule),
            new("SecurityStamp", user.SecurityStamp)
        };
        if (recoveryRequest is not null)
        {
            claims.Add(new("MustChangePassword", "true"));
            claims.Add(new("PasswordResetRequestId", recoveryRequest.Id.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = recoveryRequest?.ExpiresAt
            });
    }
}
