using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5
        };
        var sourceBox = await boxService.CreateBoxAsync(sourceBoxDto, opUser.Id);
        await boxService.OpenBoxAsync(sourceBox.Id, opUser.Id, "TEST-STATION");

        // Create Destination Box
        var destBoxDto = new CreateBoxDto
        {
            Type = BoxType.Plastic,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5
        };
        var destBox = await boxService.CreateBoxAsync(destBoxDto, opUser.Id);
        await boxService.OpenBoxAsync(destBox.Id, opUser.Id, "TEST-STATION");

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

        // Assert - exactly one dedicated transfer event contains the full business context.
        var transferLog = await db.BoxAuditLogs
            .SingleAsync(log => log.ActionType == "PackageTransferred");

        Assert.Equal(sourceBox.Id, transferLog.BoxId);
        Assert.Equal("PKG-TRANSFER-TEST-001", transferLog.PackageBarcode);
        Assert.Equal(spUser.Id, transferLog.UserId);
        Assert.Equal("TEST-STATION", transferLog.WorkstationName);
        Assert.Equal("Transfer testing reason", transferLog.Reason);
        Assert.Equal(sourceBox.Id.ToString(), transferLog.PreviousValue);
        Assert.Equal(destBox.Id.ToString(), transferLog.NewValue);
        Assert.Contains(sourceBox.BoxNumber, transferLog.DetailsJson);
        Assert.Contains(destBox.BoxNumber, transferLog.DetailsJson);
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
            Type = BoxType.Cardboard,
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

        var boxDto = new CreateBoxDto { Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);
        await boxService.OpenBoxAsync(box.Id, opUser.Id, "TEST-STATION");

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
    public async Task BlockPackage_ExcludesItFromValidQuantity_AndUnblockRestoresCompletion()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var operatorUser = db.Users.First(user => user.Matricule == "OP001");
        var supervisor = db.Users.First(user => user.Matricule == "SP001");
        var box = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 1
        }, operatorUser.Id);
        await boxService.OpenBoxAsync(box.Id, operatorUser.Id, "TEST-STATION");
        var scan = await packageScanService.ScanPackageAsync(
            box.Id, "PKG-QUARANTINE-001", operatorUser.Id, "TEST-STATION");
        Assert.True(scan.Success);
        Assert.Equal(BoxStatus.Completed, scan.Box!.Status);

        var package = await db.BoxPackages.SingleAsync(p => p.PackageBarcode == "PKG-QUARANTINE-001");
        var blocked = await boxService.BlockPackageAsync(
            package.Id, "Quality hold", supervisor.Id, "QA-STATION");

        Assert.Equal(0, blocked.CurrentQuantity);
        Assert.Equal(BoxStatus.Open, blocked.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            boxService.BlockPackageAsync(package.Id, "Repeated hold", supervisor.Id, "QA-STATION"));

        var unblocked = await boxService.UnblockPackageAsync(
            package.Id, "Quality approved", supervisor.Id, "QA-STATION");

        Assert.Equal(1, unblocked.CurrentQuantity);
        Assert.Equal(BoxStatus.Completed, unblocked.Status);
        var completedBox = await db.Boxes.AsNoTracking().SingleAsync(candidate => candidate.Id == box.Id);
        Assert.Equal(supervisor.Id, completedBox.CompletedByUserId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            boxService.UnblockPackageAsync(package.Id, "Repeated approval", supervisor.Id, "QA-STATION"));
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

        var box1 = await boxService.CreateBoxAsync(new CreateBoxDto { Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 }, opUser.Id);
        await boxService.OpenBoxAsync(box1.Id, opUser.Id, "TEST-STATION");
        var box2 = await boxService.CreateBoxAsync(new CreateBoxDto { Type = BoxType.Wood, Height = 12, Width = 12, Depth = 12, ExpectedQuantity = 10 }, opUser.Id);

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
        var boxDto = new CreateBoxDto { Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);
        await boxService.OpenBoxAsync(box.Id, opUser.Id, "TEST-STATION");
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

        // Verify traceability is retained in db
        var dbPkg = await db.BoxPackages.FirstOrDefaultAsync(p => p.Id == pkg.Id);
        Assert.NotNull(dbPkg);
        Assert.True(dbPkg.IsRemoved);
        Assert.Equal("Withdrawing from cancelled box", dbPkg.RemovalReason);
        Assert.NotNull(dbPkg.RemovedAt);
        Assert.Equal(opUser.Id, dbPkg.RemovedByUserId);
    }

    [Fact]
    public async Task DisassociatePackage_FromCancelledBox_RetainsPackageHistory()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var opUser = db.Users.First(u => u.Matricule == "OP001");

        var boxDto = new CreateBoxDto { Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 5 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);
        await boxService.OpenBoxAsync(box.Id, opUser.Id, "TEST-STATION");
        var scanResult = await packageScanService.ScanPackageAsync(box.Id, "PKG-CANCEL-DIS-001", opUser.Id, "TEST-STATION");
        Assert.True(scanResult.Success);

        await boxService.CancelBoxAsync(box.Id, "Cancel testing disassociation", opUser.Id, "TEST-STATION");

        var details = await boxService.GetBoxByIdAsync(box.Id);
        var pkg = details!.Packages.First();

        var resultBox = await boxService.DisassociatePackageAsync(pkg.Id, "Release package from cancelled box", opUser.Id, "TEST-STATION");

        Assert.Equal(0, resultBox.CurrentQuantity);
        Assert.Empty(resultBox.Packages);

        var dbPkg = await db.BoxPackages.FirstOrDefaultAsync(p => p.Id == pkg.Id);
        Assert.NotNull(dbPkg);
        Assert.True(dbPkg.IsRemoved);
        Assert.Equal("Release package from cancelled box", dbPkg.RemovalReason);
        Assert.NotNull(dbPkg.RemovedAt);
        Assert.Equal(opUser.Id, dbPkg.RemovedByUserId);
    }

    [Fact]
    public async Task DisassociatedPackage_CanBeReassociatedWhileRetainingHistory()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var user = db.Users.First(candidate => candidate.Matricule == "OP001");
        var source = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 2
        }, user.Id);
        await boxService.OpenBoxAsync(source.Id, user.Id, "SOURCE-STATION");
        Assert.True((await packageScanService.ScanPackageAsync(
            source.Id, "PKG-REASSOCIATE-001", user.Id, "SOURCE-STATION")).Success);

        await boxService.CancelBoxAsync(source.Id, "Source box cancelled", user.Id, "SOURCE-STATION");
        var originalPackage = await db.BoxPackages.SingleAsync(
            package => package.PackageBarcode == "PKG-REASSOCIATE-001");
        await boxService.DisassociatePackageAsync(
            originalPackage.Id, "Approved reassociation", user.Id, "SOURCE-STATION");

        var destination = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Plastic, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 2
        }, user.Id);
        await boxService.OpenBoxAsync(destination.Id, user.Id, "DESTINATION-STATION");

        var reassociation = await packageScanService.ScanPackageAsync(
            destination.Id, "PKG-REASSOCIATE-001", user.Id, "DESTINATION-STATION");

        Assert.True(reassociation.Success);
        var history = await db.BoxPackages
            .Where(package => package.PackageBarcode == "PKG-REASSOCIATE-001")
            .OrderBy(package => package.Id)
            .ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.True(history[0].IsRemoved);
        Assert.Equal(source.Id, history[0].BoxId);
        Assert.False(history[1].IsRemoved);
        Assert.Equal(destination.Id, history[1].BoxId);
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

        var boxDto = new CreateBoxDto { Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 1 };
        var box = await boxService.CreateBoxAsync(boxDto, opUser.Id);
        await boxService.OpenBoxAsync(box.Id, opUser.Id, "TEST-STATION");

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
    public async Task ScanPackageAsync_ReplayedRequestId_ReturnsOriginalSuccessWithoutDuplicate()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxes = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var scanner = scope.ServiceProvider.GetRequiredService<IPackageScanService>();
        db.BoxPackages.RemoveRange(db.BoxPackages);
        db.Boxes.RemoveRange(db.Boxes);
        await db.SaveChangesAsync();

        var user = db.Users.First(candidate => candidate.Matricule == "OP001");
        var box = await boxes.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Cardboard, Height = 10, Width = 10, Depth = 10, ExpectedQuantity = 3
        }, user.Id);
        await boxes.OpenBoxAsync(box.Id, user.Id, "IDEMPOTENCY-STATION");
        const string requestId = "scan-request-replay-001";

        var first = await scanner.ScanPackageAsync(
            box.Id, "PKG-IDEMPOTENT-001", user.Id, "IDEMPOTENCY-STATION", requestId: requestId);
        var replay = await scanner.ScanPackageAsync(
            box.Id, "PKG-IDEMPOTENT-001", user.Id, "IDEMPOTENCY-STATION", requestId: requestId);

        Assert.True(first.Success);
        Assert.True(replay.Success);
        Assert.Contains("already recorded", replay.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, replay.Box!.CurrentQuantity);
        Assert.Equal(1, await db.BoxPackages.CountAsync(package => package.ScanRequestId == requestId));
    }

}
