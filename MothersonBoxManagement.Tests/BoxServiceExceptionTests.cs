using Microsoft.EntityFrameworkCore;
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
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

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
        var scanResult = await packageScanService.ScanPackageAsync(sourceBox.Id, "PKG-TRANSFER-TEST-001", opUser.Id, "TEST-STATION");
        Assert.True(scanResult.Success);

        // Verify quantities before transfer
        var sourceBoxDetailsBefore = await boxService.GetBoxByIdAsync(sourceBox.Id);
        var destBoxDetailsBefore = await boxService.GetBoxByIdAsync(destBox.Id);
        Assert.Equal(1, sourceBoxDetailsBefore!.CurrentQuantity);
        Assert.Equal(0, destBoxDetailsBefore!.CurrentQuantity);

        var package = sourceBoxDetailsBefore.Packages.First();

        // Act - Transfer package
        var updatedSourceBox = await boxService.TransferPackageAsync(package.Id, destBox.Id, "Transfer testing reason", spUser.Id, "TEST-STATION");

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
        var updatedBox = await boxService.CancelBoxAsync(box.Id, "Damaged box", spUser.Id, "TEST-STATION");

        // Assert
        Assert.Equal(BoxStatus.Cancelled, updatedBox.Status);
        Assert.Equal("Damaged box", updatedBox.ExceptionReason);

        // State validation: trying to cancel a cancelled box should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            boxService.CancelBoxAsync(box.Id, "Cancel again", spUser.Id, "TEST-STATION"));
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
        var blockedBox = await boxService.BlockBoxAsync(box.Id, "Investigating contents", spUser.Id, "TEST-STATION");
        Assert.Equal(BoxStatus.Blocked, blockedBox.Status);
        Assert.Equal("Investigating contents", blockedBox.BlockReason);

        // Act - Unblock Box
        var unblockedBox = await boxService.UnblockBoxAsync(box.Id, "Investigation complete", spUser.Id, "TEST-STATION");
        Assert.Equal(BoxStatus.Open, unblockedBox.Status);
        Assert.Null(unblockedBox.BlockReason);
    }

    [Fact]
    public async Task SearchBoxes_AppliesFiltersCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");

        var box1 = await boxService.CreateBoxAsync(new CreateBoxDto { Type = BoxType.Carton, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 }, opUser.Id);
        var box2 = await boxService.CreateBoxAsync(new CreateBoxDto { Type = BoxType.Bois, Height = 12, Width = 12, Depth = 12, ExpectedQuantity = 10 }, opUser.Id);

        // Cancel box2 to have different statuses
        await boxService.CancelBoxAsync(box2.Id, "Cancel test", opUser.Id, "TEST-STATION");

        // Test filter by status (Open)
        var openBoxes = await boxService.SearchBoxesAsync(new BoxSearchFilterDto { Status = BoxStatus.Open });
        Assert.Single(openBoxes);
        Assert.Equal(box1.BoxNumber, openBoxes[0].BoxNumber);

        // Test filter by status (Cancelled)
        var cancelledBoxes = await boxService.SearchBoxesAsync(new BoxSearchFilterDto { Status = BoxStatus.Cancelled });
        Assert.Single(cancelledBoxes);
        Assert.Equal(box2.BoxNumber, cancelledBoxes[0].BoxNumber);

        // Test filter by box number partial match
        var matchedBoxes = await boxService.SearchBoxesAsync(new BoxSearchFilterDto { BoxNumber = box1.BoxNumber.Substring(0, 10) });
        Assert.Equal(2, matchedBoxes.Count);
    }

    [Fact]
    public async Task RetraitPackage_FromCancelledBox_Succeeds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");

        // Create box and scan package
        var boxDto = new CreateBoxDto { Type = BoxType.Carton, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);
        var scanResult = await packageScanService.ScanPackageAsync(box.Id, "PKG-CANCEL-RET-001", opUser.Id, "TEST-STATION");
        Assert.True(scanResult.Success);

        // Cancel box
        await boxService.CancelBoxAsync(box.Id, "Cancel testing withdrawal", opUser.Id, "TEST-STATION");

        // Get updated details and package id
        var details = await boxService.GetBoxByIdAsync(box.Id);
        Assert.Equal(BoxStatus.Cancelled, details!.Status);
        var pkg = details.Packages.First();

        // Act - Withdraw package from Cancelled box
        var resultBox = await boxService.RetraitPackageAsync(pkg.Id, "Withdrawing from cancelled box", opUser.Id, "TEST-STATION");

        // Assert
        Assert.Equal(0, resultBox.CurrentQuantity);
        Assert.Empty(resultBox.Packages);

        // Verify it is removed from db
        var dbPkg = await db.BoxPackages.FirstOrDefaultAsync(p => p.Id == pkg.Id);
        Assert.Null(dbPkg);
    }

    [Fact]
    public async Task ScanPackageAsync_Rejection_GeneratesPackageRejectedAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        db.BoxAuditLogs.RemoveRange(db.BoxAuditLogs);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");

        var boxDto = new CreateBoxDto { Type = BoxType.Carton, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 1 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);

        // Try scanning a box barcode as a package (should fail)
        var scanResult1 = await packageScanService.ScanPackageAsync(box.Id, "BOX-INVALID-SCAN", opUser.Id, "TEST-STATION");
        Assert.False(scanResult1.Success);

        // Scan normal package (should succeed)
        var scanResult2 = await packageScanService.ScanPackageAsync(box.Id, "PKG-VALID-SCAN-01", opUser.Id, "TEST-STATION");
        Assert.True(scanResult2.Success);

        // Try scanning when box is full (should fail)
        var scanResult3 = await packageScanService.ScanPackageAsync(box.Id, "PKG-TOO-MANY-SCAN", opUser.Id, "TEST-STATION");
        Assert.False(scanResult3.Success);

        // Verify audit logs contain PackageRejected
        var rejectedLogs = await db.BoxAuditLogs
            .Where(l => l.ActionType == "PackageRejected")
            .ToListAsync();

        Assert.Equal(2, rejectedLogs.Count);
        Assert.Contains(rejectedLogs, l => l.PackageBarcode == "BOX-INVALID-SCAN");
        Assert.Contains(rejectedLogs, l => l.PackageBarcode == "PKG-TOO-MANY-SCAN");
    }

    [Fact]
    public async Task LogBoxResumedIfNeeded_Reprise_DoesNotGenerateExplicitAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        db.BoxAuditLogs.RemoveRange(db.BoxAuditLogs);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");
        var spUser = db.Users.First(u => u.Matricule == "SP001");

        // opUser creates the box
        var boxDto = new CreateBoxDto { Type = BoxType.Carton, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);

        // opUser resumes -> should NOT log since opUser created it and was the last modifier
        await boxService.LogBoxResumedIfNeededAsync(box.Id, opUser.Id, "TEST-STATION");
        var resumeLogs1 = await db.BoxAuditLogs.Where(l => l.ActionType == "BoxResumed").ToListAsync();
        Assert.Empty(resumeLogs1);

        // spUser resumes -> audit logging is now handled by the interceptor, not explicit calls
        await boxService.LogBoxResumedIfNeededAsync(box.Id, spUser.Id, "TEST-STATION");
        // The interceptor does not generate "BoxResumed" action type; this is now a no-op audit-wise
        var resumeLogs2 = await db.BoxAuditLogs.Where(l => l.ActionType == "BoxResumed").ToListAsync();
        Assert.Empty(resumeLogs2);
    }
}
