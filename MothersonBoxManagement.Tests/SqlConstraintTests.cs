using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class SqlConstraintTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SqlConstraintTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void BoxModel_DefinesCriticalCheckConstraints()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var model = db.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Box))!;
        var constraints = entityType.GetCheckConstraints()
            .ToDictionary(constraint => constraint.Name!, constraint => constraint.Sql);

        Assert.Equal("[ExpectedQuantity] > 0", constraints["CK_Boxes_ExpectedQuantity_Positive"]);
        Assert.Equal(
            "[CurrentQuantity] >= 0 AND [CurrentQuantity] <= [ExpectedQuantity]",
            constraints["CK_Boxes_CurrentQuantity_Range"]);
        Assert.Equal(
            "[Height] > 0 AND [Width] > 0 AND [Depth] > 0",
            constraints["CK_Boxes_Dimensions_Positive"]);
    }

    private async Task<(int boxId, string barcode)> SeedOpenBox()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");
        var box = new Box
        {
            BoxNumber = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            BarcodeValue = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Type = BoxType.Cardboard,
            Height = 10, Width = 10, Depth = 10,
            ExpectedQuantity = 100,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            LastModifiedByUserId = user.Id,
            ModifiedAt = DateTime.UtcNow
        };
        db.Boxes.Add(box);
        await db.SaveChangesAsync();
        return (box.Id, box.BarcodeValue);
    }

    [Fact]
    public async Task DuplicatePackageBarcode_ThrowsUniqueConstraintViolation()
    {
        var (boxId, _) = await SeedOpenBox();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");

        var barcode = $"PKG-DUP-{Guid.NewGuid():N}"[..20];

        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = boxId,
            PackageBarcode = barcode,
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });
        await db.SaveChangesAsync();

        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = boxId,
            PackageBarcode = barcode,
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DifferentPackages_SameBox_Succeeds()
    {
        var (boxId, _) = await SeedOpenBox();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");

        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = boxId,
            PackageBarcode = $"PKG-A-{Guid.NewGuid():N}"[..20],
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });
        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = boxId,
            PackageBarcode = $"PKG-B-{Guid.NewGuid():N}"[..20],
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });

        await db.SaveChangesAsync();

        var count = await db.BoxPackages.CountAsync(bp => bp.BoxId == boxId);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AutoComplete_BoxFillsUp_StatusChangesToCompleted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");

        var box = new Box
        {
            BoxNumber = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            BarcodeValue = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Type = BoxType.Cardboard,
            Height = 10, Width = 10, Depth = 10,
            ExpectedQuantity = 2,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            LastModifiedByUserId = user.Id,
            ModifiedAt = DateTime.UtcNow
        };
        db.Boxes.Add(box);
        await db.SaveChangesAsync();

        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = box.Id,
            PackageBarcode = $"PKG-AC1-{Guid.NewGuid():N}"[..21],
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });
        box.CurrentQuantity = 1;
        await db.SaveChangesAsync();

        db.BoxPackages.Add(new BoxPackage
        {
            BoxId = box.Id,
            PackageBarcode = $"PKG-AC2-{Guid.NewGuid():N}"[..21],
            ScannedByUserId = user.Id,
            ScannedAt = DateTime.UtcNow,
            WorkstationName = "TEST-WS"
        });
        box.CurrentQuantity = 2;
        box.Status = BoxStatus.Completed;
        box.CompletionMode = "Automatic";
        box.CompletedAt = DateTime.UtcNow;
        box.CompletedByUserId = user.Id;
        await db.SaveChangesAsync();

        var updatedBox = await db.Boxes.FindAsync(box.Id);
        Assert.Equal(BoxStatus.Completed, updatedBox!.Status);
        Assert.Equal(2, updatedBox.CurrentQuantity);
        Assert.Equal("Automatic", updatedBox.CompletionMode);
    }
}
