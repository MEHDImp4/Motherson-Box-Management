using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace MothersonBoxManagement.Tests;

public class BoxServiceExceptionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BoxServiceExceptionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TransferPackage_SuccessfulTransfer_UpdatesQuantitiesAndCreatesAuditLog()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        // Clear existing boxes/packages to avoid collisions in InMemory DB
        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        db.BoxAuditLogs.RemoveRange(db.BoxAuditLogs);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");
        var spUser = db.Users.First(u => u.Matricule == "SP001");

        // Create Source Box
        var sourceBoxDto = new CreateBoxDto
        {
            Type = BoxType.Carton,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5
        };
        var sourceBox = await boxService.CreateBoxAsync(sourceBoxDto, opUser.Id);

        // Create Destination Box
        var destBoxDto = new CreateBoxDto
        {
            Type = BoxType.Plastique,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5
        };
        var destBox = await boxService.CreateBoxAsync(destBoxDto, opUser.Id);

        // Scan package in source box
        var scanResult = await boxService.ScanPackageAsync(sourceBox.Id, "PKG-TRANSFER-TEST-001", opUser.Id);
        Assert.True(scanResult.Success);

        // Verify quantities before transfer
        var sourceBoxDetailsBefore = await boxService.GetBoxByIdAsync(sourceBox.Id);
        var destBoxDetailsBefore = await boxService.GetBoxByIdAsync(destBox.Id);
        Assert.Equal(1, sourceBoxDetailsBefore!.CurrentQuantity);
        Assert.Equal(0, destBoxDetailsBefore!.CurrentQuantity);

        var package = sourceBoxDetailsBefore.Packages.First();

        // Act - Transfer package
        var updatedSourceBox = await boxService.TransferPackageAsync(package.Id, destBox.Id, "Transfer testing reason", spUser.Id);

        // Assert - Quantities updated correctly
        var sourceBoxDetailsAfter = await boxService.GetBoxByIdAsync(sourceBox.Id);
        var destBoxDetailsAfter = await boxService.GetBoxByIdAsync(destBox.Id);

        Assert.Equal(0, sourceBoxDetailsAfter!.CurrentQuantity);
        Assert.Equal(1, destBoxDetailsAfter!.CurrentQuantity);
        Assert.Equal("Transfer testing reason", sourceBoxDetailsAfter.ExceptionReason);
        Assert.Equal("Transfer testing reason", destBoxDetailsAfter.ExceptionReason);

        // Assert - Audit Log triggers
        var auditLogs = db.BoxAuditLogs.ToList();
        Assert.NotEmpty(auditLogs);
        
        var transferLogs = auditLogs.Where(l => l.DetailsJson != null && l.DetailsJson.Contains("PKG-TRANSFER-TEST-001")).ToList();
        Assert.NotEmpty(transferLogs);
    }
    
    [Fact]
    public async Task CancelBox_SetsStatusToCancelled_AndLogsReason()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");
        var spUser = db.Users.First(u => u.Matricule == "SP001");

        var boxDto = new CreateBoxDto
        {
            Type = BoxType.Carton,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5
        };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);

        // Act
        var updatedBox = await boxService.CancelBoxAsync(box.Id, "Damaged box", spUser.Id);

        // Assert
        Assert.Equal(BoxStatus.Cancelled, updatedBox.Status);
        Assert.Equal("Damaged box", updatedBox.ExceptionReason);

        // State validation: trying to cancel a cancelled box should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            boxService.CancelBoxAsync(box.Id, "Cancel again", spUser.Id));
    }

    [Fact]
    public async Task BlockAndUnblockBox_ChangesStatusAndLogsReason()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");
        var spUser = db.Users.First(u => u.Matricule == "SP001");

        var boxDto = new CreateBoxDto { Type = BoxType.Carton, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);

        // Act - Block Box
        var blockedBox = await boxService.BlockBoxAsync(box.Id, "Investigating contents", spUser.Id);
        Assert.Equal(BoxStatus.Blocked, blockedBox.Status);
        Assert.Equal("Investigating contents", blockedBox.ExceptionReason);

        // Act - Unblock Box
        var unblockedBox = await boxService.UnblockBoxAsync(box.Id, "Investigation complete", spUser.Id);
        Assert.Equal(BoxStatus.Open, unblockedBox.Status);
        Assert.Equal("Investigation complete", unblockedBox.ExceptionReason);
    }
}
