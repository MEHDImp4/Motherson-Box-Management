using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.PolicyNames.SupervisorOrAdmin, policy =>
        policy.RequireRole(
            AppRoles.SupervisorFr, AppRoles.AdminFr,
            AppRoles.Supervisor, AppRoles.Administrator));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>());
});

builder.Services.AddScoped<IUserAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPackageScanService, PackageScanService>();
builder.Services.AddScoped<IBoxService, BoxService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWorkstationResolver, WorkstationResolver>();
builder.Services.AddScoped<ILoginLockoutService, LoginLockoutService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
    {
        var matricule = httpContext.Request.Form["Matricule"].FirstOrDefault() ?? "unknown";
        return RateLimitPartition.GetTokenBucketLimiter(matricule, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 5,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("scan", httpContext =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1)
        });
    });

    options.AddPolicy("global", _ =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Motherson.BoxManagement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
    app.UseHsts();
}

if (app.Configuration.GetValue<int>("Kestrel:Certificates:Default:Port") is > 0 ||
    app.Configuration.GetValue<int?>("HTTPS_PORT") is > 0)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Dashboard/Error/{0}");

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .RequireRateLimiting("global");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    await DbInitializer.SeedAsync(db);
}

app.Run();
