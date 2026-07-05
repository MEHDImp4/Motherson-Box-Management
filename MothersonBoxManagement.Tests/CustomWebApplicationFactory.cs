using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.AddInterceptors(
                    sp.GetRequiredService<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>(),
                    new E2EUniqueConstraintSimulatingInterceptor()
                );
            });

            services.AddSingleton<IAntiforgery, FakeAntiforgery>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            E2EUniqueConstraintSimulatingInterceptor.ClearSeenBarcodes();

            var passwordHasher = new PasswordHasher<User>();
            const string password = "Motherson2026!";

            var users = new[]
            {
                new User { Matricule = "OP001", Role = "Operator", IsActive = true },
                new User { Matricule = "SP001", Role = "Supervisor", IsActive = true },
                new User { Matricule = "AD001", Role = "Administrator", IsActive = true }
            };

            foreach (var user in users)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, password);
            }

            db.Users.AddRange(users);
            db.SaveChanges();
        });
    }
}

public class FakeAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test_token", "test_cookie", "RequestVerificationToken", "X-CSRF-TOKEN");
    }

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test_token", "test_cookie", "RequestVerificationToken", "X-CSRF-TOKEN");
    }

    public Task<bool> IsRequestValidAsync(HttpContext httpContext)
    {
        return Task.FromResult(true);
    }

    public Task ValidateRequestAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
    }
}

public class E2EUniqueConstraintSimulatingInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> _seenBarcodes = new();
    private static readonly object _lock = new();

    public static void ClearSeenBarcodes()
    {
        lock (_lock)
        {
            _seenBarcodes.Clear();
        }
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var newPackages = eventData.Context.ChangeTracker.Entries<BoxPackage>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var pkg in newPackages)
        {
            lock (_lock)
            {
                var existsInDb = eventData.Context.Set<BoxPackage>().Any(p => p.PackageBarcode == pkg.PackageBarcode && p.Id != pkg.Id);
                if (_seenBarcodes.Contains(pkg.PackageBarcode) || existsInDb)
                {
                    var inner = new Exception("IX_BoxPackages_PackageBarcode");
                    throw new DbUpdateException("Duplicate package barcode unique index violation.", inner);
                }
                _seenBarcodes.Add(pkg.PackageBarcode);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
