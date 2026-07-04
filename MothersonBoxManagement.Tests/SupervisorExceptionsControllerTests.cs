using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class SupervisorExceptionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupervisorExceptionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAsSupervisorAsync()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Matricule", "SP001"),
            new KeyValuePair<string, string>("Password", "Motherson2026!")
        });

        var response = await client.PostAsync("/Account/Login", formData);
        if (response.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException("Login failed");

        return client;
    }

    private async Task<HttpClient> LoginAsOperatorAsync()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Matricule", "OP001"),
            new KeyValuePair<string, string>("Password", "Motherson2026!")
        });

        var response = await client.PostAsync("/Account/Login", formData);
        if (response.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException("Login failed");

        return client;
    }

    private async Task<(HttpClient Client, BoxDetailsDto Box)> CreateBoxAsync(HttpClient client)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "10"),
            new KeyValuePair<string, string>("Width", "10"),
            new KeyValuePair<string, string>("Depth", "10"),
            new KeyValuePair<string, string>("ExpectedQuantity", "5")
        });

        var response = await client.PostAsync("/Box/Create", formData);
        var detailsUrl = response.Headers.Location?.OriginalString;
        var barcode = detailsUrl!.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(barcode);
        return (client, box!);
    }

    [Fact]
    public async Task Operator_CannotAccess_SupervisorActions()
    {
        // Arrange
        var operatorClient = await LoginAsOperatorAsync();
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        var cancelForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Operator trying to cancel")
        });

        // Act
        var response = await operatorClient.PostAsync("/Box/CancelBox", cancelForm);

        // Assert
        // Operator should be redirected to Login (AccessDeniedPath is configured to /Account/Login)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Supervisor_CanCancelBox_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        var cancelForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Damaged Carton Box")
        });

        // Act
        var response = await supervisorClient.PostAsync("/Box/CancelBox", cancelForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var updatedBox = await boxService.GetBoxByIdAsync(box.Id);
        Assert.Equal(BoxStatus.Cancelled, updatedBox!.Status);
        Assert.Equal("Damaged Carton Box", updatedBox.ExceptionReason);
    }

    [Fact]
    public async Task Supervisor_CanForceCloseBox_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        var forceCloseForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "End of shift force close")
        });

        // Act
        var response = await supervisorClient.PostAsync("/Box/ForceCloseBox", forceCloseForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var updatedBox = await boxService.GetBoxByIdAsync(box.Id);
        Assert.Equal(BoxStatus.CompletedWithException, updatedBox!.Status);
        Assert.Equal("End of shift force close", updatedBox.ExceptionReason);
    }

    [Fact]
    public async Task Supervisor_CanBlockAndUnblockBox_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        var blockForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Quality check hold")
        });

        // Act - Block
        var responseBlock = await supervisorClient.PostAsync("/Box/BlockBox", blockForm);
        Assert.Equal(HttpStatusCode.Redirect, responseBlock.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
            var updatedBox = await boxService.GetBoxByIdAsync(box.Id);
            Assert.Equal(BoxStatus.Blocked, updatedBox!.Status);
            Assert.Equal("Quality check hold", updatedBox.ExceptionReason);
        }

        // Act - Unblock
        var unblockForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Quality hold released")
        });
        var responseUnblock = await supervisorClient.PostAsync("/Box/UnblockBox", unblockForm);
        Assert.Equal(HttpStatusCode.Redirect, responseUnblock.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
            var updatedBox = await boxService.GetBoxByIdAsync(box.Id);
            Assert.Equal(BoxStatus.Open, updatedBox!.Status);
            Assert.Equal("Quality hold released", updatedBox.ExceptionReason);
        }
    }

    [Fact]
    public async Task Supervisor_CanModifyExpectedQuantity_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        var modifyQtyForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("expectedQuantity", "10"),
            new KeyValuePair<string, string>("reason", "Increasing expected capacity")
        });

        // Act
        var response = await supervisorClient.PostAsync("/Box/ModifyExpectedQuantity", modifyQtyForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var updatedBox = await boxService.GetBoxByIdAsync(box.Id);
        Assert.Equal(10, updatedBox!.ExpectedQuantity);
        Assert.Equal("Increasing expected capacity", updatedBox.ExceptionReason);
    }

    [Fact]
    public async Task Supervisor_CanWithdrawPackage_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        // Scan package
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("barcode", "PKG-WITHDRAW-TEST")
        });
        await supervisorClient.PostAsync("/Box/Scan", scanForm);

        using var scope1 = _factory.Services.CreateScope();
        var boxService1 = scope1.ServiceProvider.GetRequiredService<IBoxService>();
        var boxWithPkg = await boxService1.GetBoxByIdAsync(box.Id);
        var pkg = boxWithPkg!.Packages.First(p => p.PackageBarcode == "PKG-WITHDRAW-TEST");

        var withdrawForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("packageId", pkg.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Mistake scan removal")
        });

        // Act
        var response = await supervisorClient.PostAsync("/Box/RetraitPackage", withdrawForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var boxService2 = scope2.ServiceProvider.GetRequiredService<IBoxService>();
        var boxAfterWithdraw = await boxService2.GetBoxByIdAsync(box.Id);
        Assert.Empty(boxAfterWithdraw!.Packages);
        Assert.Equal(0, boxAfterWithdraw.CurrentQuantity);
    }

    [Fact]
    public async Task Supervisor_CanBlockAndUnblockPackage_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, box) = await CreateBoxAsync(supervisorClient);

        // Scan package
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("barcode", "PKG-BLOCK-TEST")
        });
        await supervisorClient.PostAsync("/Box/Scan", scanForm);

        using var scope1 = _factory.Services.CreateScope();
        var boxService1 = scope1.ServiceProvider.GetRequiredService<IBoxService>();
        var boxWithPkg = await boxService1.GetBoxByIdAsync(box.Id);
        var pkg = boxWithPkg!.Packages.First(p => p.PackageBarcode == "PKG-BLOCK-TEST");

        var blockForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("packageId", pkg.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Damaged label hold")
        });

        // Act - Block Package
        var responseBlock = await supervisorClient.PostAsync("/Box/BlockPackage", blockForm);
        Assert.Equal(HttpStatusCode.Redirect, responseBlock.StatusCode);

        using (var scope2 = _factory.Services.CreateScope())
        {
            var boxService2 = scope2.ServiceProvider.GetRequiredService<IBoxService>();
            var updatedBox = await boxService2.GetBoxByIdAsync(box.Id);
            var updatedPkg = updatedBox!.Packages.First(p => p.Id == pkg.Id);
            Assert.True(updatedPkg.IsBlocked);
            Assert.Equal("Damaged label hold", updatedPkg.BlockReason);
        }

        // Act - Unblock Package
        var unblockForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("packageId", pkg.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", box.BarcodeValue),
            new KeyValuePair<string, string>("reason", "Label reprinted")
        });
        var responseUnblock = await supervisorClient.PostAsync("/Box/UnblockPackage", unblockForm);
        Assert.Equal(HttpStatusCode.Redirect, responseUnblock.StatusCode);

        using (var scope3 = _factory.Services.CreateScope())
        {
            var boxService3 = scope3.ServiceProvider.GetRequiredService<IBoxService>();
            var updatedBox = await boxService3.GetBoxByIdAsync(box.Id);
            var updatedPkg = updatedBox!.Packages.First(p => p.Id == pkg.Id);
            Assert.False(updatedPkg.IsBlocked);
            Assert.Equal("Label reprinted", updatedPkg.BlockReason);
        }
    }

    [Fact]
    public async Task Supervisor_CanTransferPackage_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();
        var (_, sourceBox) = await CreateBoxAsync(supervisorClient);
        var (_, destBox) = await CreateBoxAsync(supervisorClient);

        // Scan package to source box
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", sourceBox.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", sourceBox.BarcodeValue),
            new KeyValuePair<string, string>("barcode", "PKG-TRANSFER-TEST")
        });
        await supervisorClient.PostAsync("/Box/Scan", scanForm);

        using var scope1 = _factory.Services.CreateScope();
        var boxService1 = scope1.ServiceProvider.GetRequiredService<IBoxService>();
        var sourceBoxWithPkg = await boxService1.GetBoxByIdAsync(sourceBox.Id);
        var pkg = sourceBoxWithPkg!.Packages.First(p => p.PackageBarcode == "PKG-TRANSFER-TEST");

        var transferForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("packageId", pkg.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", sourceBox.BarcodeValue),
            new KeyValuePair<string, string>("destinationBoxId", destBox.Id.ToString()),
            new KeyValuePair<string, string>("reason", "Transferring package to correct box")
        });

        // Act
        var response = await supervisorClient.PostAsync("/Box/TransferPackage", transferForm);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var boxService2 = scope2.ServiceProvider.GetRequiredService<IBoxService>();
        var updatedSource = await boxService2.GetBoxByIdAsync(sourceBox.Id);
        var updatedDest = await boxService2.GetBoxByIdAsync(destBox.Id);

        Assert.Empty(updatedSource!.Packages);
        Assert.Equal(0, updatedSource.CurrentQuantity);
        Assert.Single(updatedDest!.Packages);
        Assert.Equal(1, updatedDest.CurrentQuantity);
        Assert.Equal("PKG-TRANSFER-TEST", updatedDest.Packages.First().PackageBarcode);
    }

    [Fact]
    public async Task AuditIndex_LoadLogsAndFiltering_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();

        // Act - Fetch index view
        var response = await supervisorClient.GetAsync("/Audit/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Journal d'Audit", content);
    }

    [Fact]
    public async Task BoxDetails_AsSupervisor_PopulatesOpenBoxesDropdown()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();

        using var scopeSetup = _factory.Services.CreateScope();
        var dbSetup = scopeSetup.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // clear existing boxes for clean dropdown assertion
        dbSetup.Boxes.RemoveRange(dbSetup.Boxes);
        await dbSetup.SaveChangesAsync();

        var supervisorUser = await dbSetup.Users.FirstAsync(u => u.Matricule == "SP001");

        var sourceBoxDto = new CreateBoxDto
        {
            Type = BoxType.Carton,
            Height = 30,
            Width = 20,
            Depth = 15,
            ExpectedQuantity = 10
        };

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var sourceBox = await boxService.CreateBoxAsync(sourceBoxDto, supervisorUser.Id);
        var destBox = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Carton,
            Height = 30,
            Width = 20,
            Depth = 15,
            ExpectedQuantity = 10
        }, supervisorUser.Id);

        // Add a package to sourceBox so the packages loop and transfer modal render
        using (var scopePkg = _factory.Services.CreateScope())
        {
            var db = scopePkg.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbBox = await db.Boxes.FindAsync(sourceBox.Id);
            dbBox!.CurrentQuantity = 1;
            db.BoxPackages.Add(new BoxPackage
            {
                BoxId = sourceBox.Id,
                PackageBarcode = "PKG-TEST-TRANSFER-OPTION-001",
                ScannedByUserId = supervisorUser.Id,
                ScannedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
        }

        // Act - Fetch details of sourceBox
        var response = await supervisorClient.GetAsync($"/Box/Details/{sourceBox.BarcodeValue}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        // Dropdown option should contain destination box number
        Assert.Contains(destBox.BoxNumber, content);
    }

    [Fact]
    public async Task AuditLog_ChangedPropertiesOnly_SerializesEnumsAsStrings()
    {
        // Arrange
        using var scopeSetup = _factory.Services.CreateScope();
        var dbSetup = scopeSetup.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var supervisorUser = await dbSetup.Users.FirstAsync(u => u.Matricule == "SP001");

        var boxDto = new CreateBoxDto
        {
            Type = BoxType.Carton,
            Height = 30,
            Width = 20,
            Depth = 15,
            ExpectedQuantity = 10
        };

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        
        var box = await boxService.CreateBoxAsync(boxDto, supervisorUser.Id);

        // Fetch audit logs from DB
        var auditLogs = await db.BoxAuditLogs
            .Where(l => l.BoxId == box.Id)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        var insertLog = auditLogs.First(l => l.ActionType == "Insert");
        Assert.NotNull(insertLog.DetailsJson);

        // Assert JSON structure: should have ChangedProperties and not OriginalValues or CurrentValues in top level
        using var doc = System.Text.Json.JsonDocument.Parse(insertLog.DetailsJson);
        var root = doc.RootElement;
        
        Assert.True(root.TryGetProperty("ChangedProperties", out var changedProps));
        Assert.False(root.TryGetProperty("OriginalValues", out _));
        Assert.False(root.TryGetProperty("CurrentValues", out _));

        // Enums should serialize as strings (e.g. Type = "Carton", Status = "Open")
        Assert.Equal("Carton", changedProps.GetProperty("Type").GetString());
        Assert.Equal("Open", changedProps.GetProperty("Status").GetString());
    }
}
