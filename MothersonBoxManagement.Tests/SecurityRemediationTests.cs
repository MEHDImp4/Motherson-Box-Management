using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MothersonBoxManagement.Configuration;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class SecurityRemediationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityRemediationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResetPassword_AuditNeverContainsPasswordHashOrPasswordValue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var user = await db.Users.SingleAsync(candidate => candidate.Matricule == "OP001");
        const string newPassword = "AuditMustNeverSeeThis-2026!";

        await users.ResetPasswordAsync(user.Id, newPassword);

        var audit = await db.BoxAuditLogs
            .Where(log => log.ActionType == "Update")
            .OrderByDescending(log => log.Id)
            .FirstAsync();
        Assert.DoesNotContain("PasswordHash", audit.DetailsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(newPassword, audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("AQAAAA", audit.DetailsJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionConfiguration_RejectsDemoUserSeeding()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=test;User Id=test;Password=test",
            ["AllowedHosts"] = "localhost",
            ["Kestrel:Certificates:Default:Path"] = "tls.pfx",
            ["Kestrel:Certificates:Default:Password"] = "certificate-password",
            ["SeedDemoUsers"] = "true"
        });

        var error = Assert.Throws<InvalidOperationException>(() =>
            builder.ValidateProductionConfiguration());

        Assert.Contains("SeedDemoUsers", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DemoSeed_RequiresThreeDistinctExternalPasswords()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoUsers:OP001:Password"] = "Operator-Only-Password-2026!",
                ["DemoUsers:SP001:Password"] = "Supervisor-Only-Password-2026!",
                ["DemoUsers:AD001:Password"] = "Administrator-Only-Password-2026!"
            })
            .Build();

        await DbInitializer.SeedAsync(db, configuration);

        var users = await db.Users
            .Where(user => user.Matricule != "SYSTEM")
            .OrderBy(user => user.Matricule)
            .ToListAsync();
        Assert.Equal(3, users.Count);
        Assert.Equal(3, users.Select(user => user.PasswordHash).Distinct().Count());
    }

    [Fact]
    public async Task DemoSeed_FailsClosedWhenAnyPasswordIsMissing()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoUsers:OP001:Password"] = "Operator-Only-Password-2026!"
            })
            .Build();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DbInitializer.SeedAsync(db, configuration));

        Assert.Contains("DemoUsers", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginLockout_ConcurrentFailuresCannotLoseIncrements()
    {
        var lockout = _factory.Services.GetRequiredService<ILoginLockoutService>();
        var matricule = $"LOCK-{Guid.NewGuid():N}"[..20];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => lockout.RecordFailedAttemptAsync(matricule, timeout.Token)));

        Assert.True(await lockout.IsLockedOutAsync(matricule, timeout.Token));
        Assert.Equal(0, await lockout.GetRemainingAttemptsAsync(matricule, timeout.Token));
    }
}
