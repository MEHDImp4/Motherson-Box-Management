using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class AutoScanPackageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AutoScanPackageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedTemplate(string prefix, string name = "Test Template", bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var template = new BoxTemplate
        {
            Name = name,
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5,
            PackagePrefixPattern = prefix,
            IsActive = isActive,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };
        db.BoxTemplates.Add(template);
        await db.SaveChangesAsync();
    }

    private async Task CleanupTemplates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.BoxTemplates.RemoveRange(db.BoxTemplates);
        await db.SaveChangesAsync();
    }

    private async Task CleanupBoxesAndPackages()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AutoScanPackage_MatchingPrefix_CreatesBoxAndAssociates()
    {
        await CleanupTemplates();
        await CleanupBoxesAndPackages();
        await SeedTemplate("12345");

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "12345AAA001" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(json.GetProperty("noMatch").GetBoolean());
        Assert.NotNull(json.GetProperty("boxNumber").GetString());
        Assert.Contains("auto-created", json.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);

        await CleanupTemplates();
    }

    [Fact]
    public async Task AutoScanPackage_NoMatch_ReturnsNoMatch()
    {
        await CleanupTemplates();
        await CleanupBoxesAndPackages();
        await SeedTemplate("99999");

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "123456789" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.GetProperty("noMatch").GetBoolean());

        await CleanupTemplates();
    }

    [Fact]
    public async Task AutoScanPackage_LongestPrefixWins()
    {
        await CleanupTemplates();
        await CleanupBoxesAndPackages();
        await SeedTemplate("123", "Short Prefix");
        await SeedTemplate("12345", "Long Prefix");

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "123456789" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Contains("Long Prefix", json.GetProperty("message").GetString());

        await CleanupTemplates();
    }

    [Fact]
    public async Task AutoScanPackage_InactiveTemplate_NotMatched()
    {
        await CleanupTemplates();
        await CleanupBoxesAndPackages();
        await SeedTemplate("12345", isActive: false);

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "123456789" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.GetProperty("noMatch").GetBoolean());

        await CleanupTemplates();
    }

    [Fact]
    public async Task AutoScanPackage_EmptyBarcode_ReturnsError()
    {
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(json.GetProperty("noMatch").GetBoolean());
    }

    [Fact]
    public async Task AutoScanPackage_BoxBarcode_ReturnsError()
    {
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "BOX-20260709-ABC123" }
        });

        var response = await client.PostAsync("/Box/AutoScanPackage", formData);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(json.GetProperty("noMatch").GetBoolean());
    }

    [Fact]
    public async Task AutoScanPackage_FirstScanFails_DoesNotPersistBoxOrPrintJob()
    {
        await CleanupTemplates();
        await CleanupBoxesAndPackages();
        await SeedTemplate("FAIL-");

        int printerId;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var printer = new PrinterConfiguration
            {
                Code = $"ATOMIC-{Guid.NewGuid():N}",
                PcName = "TEST-PC",
                PrinterName = "Test printer",
                PrinterUncPath = @"\\test-pc\zebra",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            seedDb.PrinterConfigurations.Add(printer);
            await seedDb.SaveChangesAsync();
            printerId = printer.Id;
        }

        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPackageScanService>();
                services.AddScoped<IPackageScanService, RejectingPackageScanService>();
            });
        });

        var client = await TestAuthHelper.CreateAuthenticatedClient(factory, "SP001", "Motherson2026!");
        var response = await client.PostAsync("/Box/AutoScanPackage", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "FAIL-0001" },
            { "workstationId", printerId.ToString() }
        }));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Boxes.AnyAsync());
        Assert.False(await db.BoxPrintJobs.AnyAsync());
    }

    private sealed class RejectingPackageScanService : IPackageScanService
    {
        public Task<ScanResult> ScanPackageAsync(
            int boxId,
            string barcode,
            int userId,
            string workstationName,
            CancellationToken cancellationToken = default,
            string? requestId = null)
        {
            return Task.FromResult(new ScanResult
            {
                Success = false,
                Message = "Simulated first-scan failure."
            });
        }
    }
}
