using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
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
                options.UseInMemoryDatabase("TestDb");
                options.AddInterceptors(sp.GetRequiredService<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>());
            });

            services.AddSingleton<IAntiforgery, FakeAntiforgery>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

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
