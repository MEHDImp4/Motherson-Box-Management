using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using MothersonBoxManagement.Configuration;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;
using MothersonBoxManagement.Services;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using MothersonBoxManagement.Printing;

var builder = WebApplication.CreateBuilder(args);
builder.ValidateProductionConfiguration();

builder.Logging.AddSimpleConsole(options =>
{
    options.ColorBehavior = LoggerColorBehavior.Enabled;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
{
    var dataProtection = builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("MothersonBoxManagement");
    if (builder.Environment.IsProduction())
    {
        dataProtection.ProtectKeysWithCertificate(new X509Certificate2(
            builder.Configuration["DataProtection:CertificatePath"]!,
            builder.Configuration["DataProtection:CertificatePassword"]!));
    }
}
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SupervisorOrAdministrator", policy =>
        policy.RequireRole(AppRoles.Supervisor, AppRoles.SupervisorFr, AppRoles.Administrator, AppRoles.AdminFr));
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
builder.Services.AddScoped<IBoxQueryService>(sp => (BoxService)sp.GetRequiredService<IBoxService>());
builder.Services.AddScoped<IBoxLifecycleService>(sp => (BoxService)sp.GetRequiredService<IBoxService>());
builder.Services.AddScoped<IBoxPackageService>(sp => (BoxService)sp.GetRequiredService<IBoxService>());
builder.Services.AddScoped<IBoxTemplateService, BoxTemplateService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IWorkstationResolver, WorkstationResolver>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<ILoginLockoutService, LoginLockoutService>();
builder.Services.AddHostedService<LoginLockoutCleanupService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICspNonceService, CspNonceService>();
builder.Services.AddScoped<IPrintAgentService, PrintAgentService>();

builder.Services.AddAppRateLimiting();
builder.Services.AddAppAuthentication(builder.Environment);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else if (app.Configuration.GetValue<int>("Kestrel:Certificates:Default:Port") is > 0 ||
    app.Configuration.GetValue<int?>("HTTPS_PORT") is > 0)
{
    app.UseHttpsRedirection();
}
app.UseResponseCompression();
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Dashboard/Error/{0}");

app.UseRateLimiter();

app.UseSecurityHeaders();

app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim("MustChangePassword", "true") &&
        !context.Request.Path.StartsWithSegments("/Account/ChangePassword") &&
        !context.Request.Path.StartsWithSegments("/Account/Logout"))
    {
        context.Response.Redirect("/Account/ChangePassword");
        return;
    }
    await next();
});
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .RequireRateLimiting("global");

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

async Task<IResult> Readiness(ApplicationDbContext db, CancellationToken cancellationToken)
{
    if (!db.Database.IsRelational())
        return Results.Ok(new { status = "Healthy" });

    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeout.CancelAfter(TimeSpan.FromSeconds(3));
    var canConnect = await db.Database.CanConnectAsync(timeout.Token);
    return canConnect
        ? Results.Ok(new { status = "Healthy" })
        : Results.Problem("Database is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
}

app.MapGet("/health", Readiness).AllowAnonymous();
app.MapGet("/health/ready", Readiness).AllowAnonymous();

await app.InitializeDatabaseAsync();

app.Run();
