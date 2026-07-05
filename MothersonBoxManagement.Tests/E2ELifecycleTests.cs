using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class E2ELifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public E2ELifecycleTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullBoxLifecycle_E2E_Succeeds()
    {
        // 1. Authenticate an Operator client (OP001) and a Supervisor client (SP001)
        var opClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var spClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        // 2. Create a box as Operator (ExpectedQuantity = 3, Carton)
        var createResponse = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "3" }
        }));
        
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var location = createResponse.Headers.Location?.ToString() ?? "";
        var barcode = location.Substring(location.LastIndexOf('/') + 1);
        Assert.StartsWith("BOX-", barcode);

        // Fetch Box ID from DB
        int boxId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == barcode);
            Assert.NotNull(box);
            boxId = box.Id;
        }

        // 3. Scan package PKG-E2E-001 and PKG-E2E-002 using Operator client
        var scan1 = await opClient.PostAsync("/Box/Scan", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PKG-E2E-001" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, scan1.StatusCode);

        var scan2 = await opClient.PostAsync("/Box/Scan", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PKG-E2E-002" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, scan2.StatusCode);

        // 4. Block the box as Supervisor with reason "Quality check in progress"
        var block = await spClient.PostAsync("/Box/BlockBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "Quality check in progress" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, block.StatusCode);

        // 5. Attempt to scan PKG-E2E-003 to the blocked box as Operator
        var scanAjax = await opClient.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PKG-E2E-003" }
        }));
        Assert.Equal(HttpStatusCode.OK, scanAjax.StatusCode);
        var ajaxJson = await scanAjax.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":false", ajaxJson);
        Assert.Contains("not open", ajaxJson); // "This box is not open for scanning."

        // 6. Unblock the box as Supervisor with reason "Released after inspection"
        var unblock = await spClient.PostAsync("/Box/UnblockBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "Released after inspection" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, unblock.StatusCode);

        // 7. Transfer package PKG-E2E-002 to another open box as Supervisor
        // First, create the destination box
        var createDest = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Plastique" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "5" }
        }));
        var destLocation = createDest.Headers.Location?.ToString() ?? "";
        var destBarcode = destLocation.Substring(destLocation.LastIndexOf('/') + 1);

        int destBoxId;
        int packageId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var destBox = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == destBarcode);
            Assert.NotNull(destBox);
            destBoxId = destBox.Id;

            var package = await db.BoxPackages.FirstOrDefaultAsync(p => p.PackageBarcode == "PKG-E2E-002");
            Assert.NotNull(package);
            packageId = package.Id;
        }

        // Call TransferPackage
        var transfer = await spClient.PostAsync("/Box/TransferPackage", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "packageId", packageId.ToString() },
            { "boxBarcode", barcode },
            { "destinationBoxId", destBoxId.ToString() },
            { "reason", "E2E package transfer" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, transfer.StatusCode);

        // 8. Force-close the box as Supervisor with reason "End of shift - Early close"
        var forceClose = await spClient.PostAsync("/Box/ForceCloseBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "End of shift - Early close" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, forceClose.StatusCode);

        // 9. Fetch box state and verify status and exception reason
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.Include(b => b.Packages).FirstOrDefaultAsync(b => b.Id == boxId);
            Assert.NotNull(box);
            Assert.Equal(BoxStatus.CompletedWithException, box.Status);
            Assert.Equal("End of shift - Early close", box.ExceptionReason);

            // 10. Retrieve the BoxAuditLogs. Assert that all entries exist, and verify DetailsJson
            // Since package transfer logs are associated with the destination box ID, we fetch all audit logs for either boxId or destBoxId.
            var logs = await db.BoxAuditLogs
                .Where(l => l.BoxId == boxId || l.BoxId == destBoxId)
                .ToListAsync();
            Assert.NotEmpty(logs);

            Assert.Contains(logs, l => l.ActionType == "BoxCreated" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "PackageScanned" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "BoxBlocked" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "PackageRejected" && l.BoxId == boxId && l.PackageBarcode == "PKG-E2E-003");
            Assert.Contains(logs, l => l.ActionType == "BoxUnblocked" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "BoxCompletedWithException" && l.BoxId == boxId);

            // Verify transfer occurred by checking the package moved to the destination box
            var transferredPackage = await db.BoxPackages.FirstOrDefaultAsync(p => p.PackageBarcode == "PKG-E2E-002");
            Assert.NotNull(transferredPackage);
            Assert.Equal(destBoxId, transferredPackage.BoxId);

            var createLog = logs.First(l => l.ActionType == "BoxCreated" && l.BoxId == boxId);
            Assert.Contains("ExpectedQuantity", createLog.DetailsJson ?? "");

            var scanLog = logs.First(l => l.ActionType == "PackageScanned" && l.BoxId == boxId);
            Assert.Contains("PKG-E2E-001", scanLog.DetailsJson ?? "");

            var sourceUpdateLogs = logs.Where(l => l.ActionType == "BoxUpdated" && l.BoxId == boxId).ToList();
            Assert.Contains(sourceUpdateLogs, l => l.Reason == "E2E package transfer");
        }
    }

    [Fact]
    public async Task ScanDuplicatePackage_FailsValidation()
    {
        var opClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // 1. Create a box (ExpectedQuantity = 3, Carton)
        var createResponse = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "3" }
        }));
        
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var location = createResponse.Headers.Location?.ToString() ?? "";
        var barcode = location.Substring(location.LastIndexOf('/') + 1);

        int boxId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == barcode);
            Assert.NotNull(box);
            boxId = box.Id;
        }

        // 2. Scan package PKG-E2E-DUP for the first time
        var scan1 = await opClient.PostAsync("/Box/Scan", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PKG-E2E-DUP" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, scan1.StatusCode);

        // 3. Scan the same package barcode via Ajax to verify duplicate validation error
        var scanAjaxSame = await opClient.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PKG-E2E-DUP" }
        }));
        Assert.Equal(HttpStatusCode.OK, scanAjaxSame.StatusCode);
        var jsonSame = await scanAjaxSame.Content.ReadAsStringAsync();
        using (var jsonDoc = JsonDocument.Parse(jsonSame))
        {
            var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
            var messageVal = jsonDoc.RootElement.GetProperty("message").GetString() ?? "";
            Assert.False(successVal);
            Assert.Contains("already been scanned", messageVal);
        }

        // 4. Create another box and try to scan the same package barcode there
        var createResponse2 = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "3" }
        }));
        var location2 = createResponse2.Headers.Location?.ToString() ?? "";
        var barcode2 = location2.Substring(location2.LastIndexOf('/') + 1);

        int boxId2;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == barcode2);
            Assert.NotNull(box);
            boxId2 = box.Id;
        }

        var scanAjaxOther = await opClient.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId2.ToString() },
            { "boxBarcode", barcode2 },
            { "barcode", "PKG-E2E-DUP" }
        }));
        Assert.Equal(HttpStatusCode.OK, scanAjaxOther.StatusCode);
        var jsonOther = await scanAjaxOther.Content.ReadAsStringAsync();
        using (var jsonDoc = JsonDocument.Parse(jsonOther))
        {
            var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
            var messageVal = jsonDoc.RootElement.GetProperty("message").GetString() ?? "";
            Assert.False(successVal);
            Assert.Contains("already been scanned", messageVal);
        }
    }

    [Fact]
    public async Task ScanInvalidPackageBarcodes_FailsValidation()
    {
        var opClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // Create a box
        var createResponse = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "3" }
        }));
        
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var location = createResponse.Headers.Location?.ToString() ?? "";
        var barcode = location.Substring(location.LastIndexOf('/') + 1);

        int boxId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == barcode);
            Assert.NotNull(box);
            boxId = box.Id;
        }

        // 1. Scan a box barcode as a package (should fail validation)
        var scanBoxBarcode = await opClient.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "BOX-20260704-123456" }
        }));
        Assert.Equal(HttpStatusCode.OK, scanBoxBarcode.StatusCode);
        var jsonBox = await scanBoxBarcode.Content.ReadAsStringAsync();
        using (var jsonDoc = JsonDocument.Parse(jsonBox))
        {
            var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
            var messageVal = jsonDoc.RootElement.GetProperty("message").GetString() ?? "";
            Assert.False(successVal);
            Assert.Contains("Box barcodes cannot be scanned as packages", messageVal);
        }

        // 2. Scan a barcode that is too short
        var scanShortBarcode = await opClient.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", "PK" }
        }));
        Assert.Equal(HttpStatusCode.OK, scanShortBarcode.StatusCode);
        var jsonShort = await scanShortBarcode.Content.ReadAsStringAsync();
        using (var jsonDoc = JsonDocument.Parse(jsonShort))
        {
            var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
            var messageVal = jsonDoc.RootElement.GetProperty("message").GetString() ?? "";
            Assert.False(successVal);
            Assert.Contains("The barcode must contain at least 3 characters", messageVal);
        }
    }

    [Fact]
    public async Task UnauthorizedAndForbiddenAccess_RedirectsToLogin()
    {
        // Unauthenticated client
        var anonymousClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await anonymousClient.GetAsync("/Box/Create");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");

        // Operator client trying to access supervisor-only endpoint (e.g. BlockBox)
        var opClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var blockResponse = await opClient.PostAsync("/Box/BlockBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", "1" },
            { "boxBarcode", "BOX-1234" },
            { "reason", "Unauthorized action" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, blockResponse.StatusCode);
        Assert.Contains("/Account/Login", blockResponse.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task ScanSamePackageSimultaneously_OnlyOneSucceeds()
    {
        // Setup multiple authenticated operator clients
        var clients = new List<HttpClient>();
        for (int i = 0; i < 5; i++)
        {
            clients.Add(await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!"));
        }

        // Create a box
        var opClient = clients[0];
        var createResponse = await opClient.PostAsync("/Box/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "10" },
            { "Width", "15" },
            { "Depth", "20" },
            { "ExpectedQuantity", "5" }
        }));
        
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var location = createResponse.Headers.Location?.ToString() ?? "";
        var barcode = location.Substring(location.LastIndexOf('/') + 1);

        int boxId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.FirstOrDefaultAsync(b => b.BarcodeValue == barcode);
            Assert.NotNull(box);
            boxId = box.Id;
        }

        const string concurrentBarcode = "PKG-E2E-CONCUR";

        // Launch 5 concurrent HTTP POST requests to scan the same package
        var tasks = clients.Select(client => client.PostAsync("/Box/ScanAjax", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "barcode", concurrentBarcode }
        }))).ToList();

        var responses = await Task.WhenAll(tasks);

        int successCount = 0;
        int failureCount = 0;

        foreach (var resp in responses)
        {
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var content = await resp.Content.ReadAsStringAsync();
            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
                if (successVal)
                {
                    successCount++;
                }
                else
                {
                    var messageVal = jsonDoc.RootElement.GetProperty("message").GetString() ?? "";
                    failureCount++;
                    Assert.Contains("already been scanned", messageVal);
                }
            }
        }

        // Exactly 1 should succeed, and 4 should fail because of unique constraint
        Assert.Equal(1, successCount);
        Assert.Equal(4, failureCount);
    }
}
