using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.ViewModels;

namespace MothersonBoxManagement.Controllers;

public class AccountController : Controller
{
    private readonly IUserAuthenticationService _authenticationService;
    private readonly ILoginLockoutService _lockoutService;

    public AccountController(IUserAuthenticationService authenticationService, ILoginLockoutService lockoutService)
    {
        _authenticationService = authenticationService;
        _lockoutService = lockoutService;
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

        if (_lockoutService.IsLockedOut(model.Matricule))
        {
            var remaining = _lockoutService.GetRemainingAttempts(model.Matricule);
            ModelState.AddModelError(string.Empty, "Account temporarily locked due to too many failed attempts. Please try again later.");
            return View(model);
        }

        var user = await _authenticationService.ValidateCredentialsAsync(model.Matricule, model.Password, cancellationToken);

        if (user is null)
        {
            _lockoutService.RecordFailedAttempt(model.Matricule);
            var remaining = _lockoutService.GetRemainingAttempts(model.Matricule);
            if (remaining > 0)
                ModelState.AddModelError(string.Empty, $"Incorrect matricule or password. {remaining} attempt(s) remaining before lockout.");
            else
                ModelState.AddModelError(string.Empty, "Account temporarily locked due to too many failed attempts. Please try again later.");
            return View(model);
        }

        _lockoutService.ResetAttempts(model.Matricule);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Matricule),
            new(ClaimTypes.Role, user.Role),
            new("Matricule", user.Matricule)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
