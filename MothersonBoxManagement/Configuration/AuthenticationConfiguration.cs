using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;

namespace MothersonBoxManagement.Configuration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Motherson.BoxManagement.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var cookieStamp = context.Principal?.FindFirst("SecurityStamp")?.Value;
                        if (!int.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(cookieStamp))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                        if (user is null || !user.IsActive || user.SecurityStamp != cookieStamp)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    }
                };
            });

        return services;
    }
}
