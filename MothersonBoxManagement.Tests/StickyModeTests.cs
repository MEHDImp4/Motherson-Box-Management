using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class StickyModeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StickyModeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int boxId, string barcode)> SeedOpenBox(int expectedQty = 10)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");

        var barcode = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var box = new Box
        {
            BoxNumber = barcode,
            BarcodeValue = barcode,
            Type = BoxType.Cardboard,
            Height = 10, Width = 10, Depth = 10,
            ExpectedQuantity = expectedQty,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            LastModifiedByUserId = user.Id,
            ModifiedAt = DateTime.UtcNow
        };
        db.Boxes.Add(box);
        await db.SaveChangesAsync();
        return (box.Id, barcode);
    }

    private async Task CleanupTemplates()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.BoxTemplates.RemoveRange(db.BoxTemplates);
        await db.SaveChangesAsync();
    }

    private async Task SeedTemplate(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.BoxTemplates.Add(new BoxTemplate
        {
            Name = $"Template-{prefix}",
            Type = BoxType.Cardboard,
            Height = 10, Width = 10, Depth = 10,
            ExpectedQuantity = 5,
            PackagePrefixPattern = prefix,
            IsActive = true,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task StickyMode_SecondPackageGoesToSameBox()
    {
        await CleanupTemplates();
        var (boxId, boxBarcode) = await SeedOpenBox();

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var pkg1 = $"PKG-SM1-{Guid.NewGuid():N}"[..20];
        var pkg2 = $"PKG-SM2-{Guid.NewGuid():N}"[..20];

        var formData1 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", pkg1 },
            { "boxBarcode", boxBarcode }
        });
        var resp1 = await client.PostAsync("/Box/AssociatePackage", formData1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var json1 = await resp1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json1.GetProperty("success").GetBoolean());

        var formData2 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", pkg2 },
            { "boxBarcode", boxBarcode }
        });
        var resp2 = await client.PostAsync("/Box/AssociatePackage", formData2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var json2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json2.GetProperty("success").GetBoolean());
        Assert.Equal(2, json2.GetProperty("currentQuantity").GetInt32());
    }

    [Fact]
    public async Task StickyMode_AutoScanThenManualAssociate_SameBox()
    {
        await CleanupTemplates();
        await SeedTemplate("STICKY");

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData1 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "STICKY001" }
        });
        var resp1 = await client.PostAsync("/Box/AutoScanPackage", formData1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var json1 = await resp1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json1.GetProperty("success").GetBoolean());
        var boxNumber = json1.GetProperty("boxNumber").GetString()!;

        var formData2 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", "STICKY002" },
            { "boxBarcode", boxNumber }
        });
        var resp2 = await client.PostAsync("/Box/AssociatePackage", formData2);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var json2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json2.GetProperty("success").GetBoolean());
        Assert.Equal(2, json2.GetProperty("currentQuantity").GetInt32());

        await CleanupTemplates();
    }

    [Fact]
    public async Task StickyMode_BoxCompleted_ReturnsCompletedStatus()
    {
        await CleanupTemplates();
        var (boxId, boxBarcode) = await SeedOpenBox(expectedQty: 2);

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var pkg1 = $"PKG-SC1-{Guid.NewGuid():N}"[..20];
        var pkg2 = $"PKG-SC2-{Guid.NewGuid():N}"[..20];

        var formData1 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", pkg1 },
            { "boxBarcode", boxBarcode }
        });
        await client.PostAsync("/Box/AssociatePackage", formData1);

        var formData2 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageBarcode", pkg2 },
            { "boxBarcode", boxBarcode }
        });
        var resp2 = await client.PostAsync("/Box/AssociatePackage", formData2);
        var json2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json2.GetProperty("success").GetBoolean());
        Assert.Equal("Completed", json2.GetProperty("status").GetString());
    }
}
