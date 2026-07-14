using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
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

    private async Task<(HttpClient Client, BoxDetailsDto Box)> CreateBoxAsync(HttpClient client, bool openBox = true)
    {
        var templateForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", "Test " + Guid.NewGuid().ToString("N")[..6]),
            new KeyValuePair<string, string>("Type", "Cardboard"),
            new KeyValuePair<string, string>("Height", "10"),
            new KeyValuePair<string, string>("Width", "10"),
            new KeyValuePair<string, string>("Depth", "10"),
            new KeyValuePair<string, string>("ExpectedQuantity", "5")
        });

        await client.PostAsync("/BoxTemplate/Create", templateForm);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var template = await db.BoxTemplates.OrderByDescending(t => t.Id).FirstAsync();

        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("templateId", template.Id.ToString())
        });

        var response = await client.PostAsync("/Box/CreateFromTemplate", createForm);
        var detailsUrl = response.Headers.Location?.OriginalString;
        var barcode = detailsUrl!.Split('/').Last();

        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(barcode);

        if (!openBox && box != null)
        {
            box = await boxService.GetBoxByBarcodeAsync(barcode);
        }

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
            Assert.Equal("Quality check hold", updatedBox.BlockReason);
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
            Assert.Null(updatedBox.BlockReason);
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
    public async Task AuditIndex_LoadLogsAndFiltering_Successfully()
    {
        // Arrange
        var supervisorClient = await LoginAsSupervisorAsync();

        // Act - Fetch index view
        var response = await supervisorClient.GetAsync("/Audit/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Audit Log", content);
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
            Type = BoxType.Cardboard,
            Height = 30,
            Width = 20,
            Depth = 15,
            ExpectedQuantity = 10
        };

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var sourceBox = await boxService.CreateBoxAsync(sourceBoxDto, supervisorUser.Id);
        await boxService.OpenBoxAsync(sourceBox.Id, supervisorUser.Id, "TEST-STATION");
        var destBox = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Cardboard,
            Height = 30,
            Width = 20,
            Depth = 15,
            ExpectedQuantity = 10
        }, supervisorUser.Id);
        await boxService.OpenBoxAsync(destBox.Id, supervisorUser.Id, "TEST-STATION");

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
            Type = BoxType.Cardboard,
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
        var insertLog = auditLogs.First(l => l.ActionType == "BoxCreated");
        Assert.NotNull(insertLog.DetailsJson);

        // Assert JSON structure: should have ChangedProperties and not OriginalValues or CurrentValues in top level
        using var doc = System.Text.Json.JsonDocument.Parse(insertLog.DetailsJson);
        var root = doc.RootElement;
        
        Assert.True(root.TryGetProperty("ChangedProperties", out var changedProps));
        Assert.False(root.TryGetProperty("OriginalValues", out _));
        Assert.False(root.TryGetProperty("CurrentValues", out _));

        // Enums should serialize as strings (e.g. Type = "Cardboard", Status = "Created")
        Assert.Equal("Cardboard", changedProps.GetProperty("Type").GetString());
        Assert.Equal("Created", changedProps.GetProperty("Status").GetString());
    }
}
