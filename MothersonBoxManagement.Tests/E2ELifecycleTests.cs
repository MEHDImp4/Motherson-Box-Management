using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        // 4. Block the box as Supervisor with reason "Contrôle qualité en cours"
        var block = await spClient.PostAsync("/Box/BlockBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "Contrôle qualité en cours" }
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
        Assert.Contains("ouverte", ajaxJson); // "Cette box n'est pas ouverte aux scans."

        // 6. Unblock the box as Supervisor with reason "Libération après inspection"
        var unblock = await spClient.PostAsync("/Box/UnblockBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "Libération après inspection" }
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
            { "reason", "Transfer de package E2E" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, transfer.StatusCode);

        // 8. Force-close the box as Supervisor with reason "Fin de poste - Clôture anticipée"
        var forceClose = await spClient.PostAsync("/Box/ForceCloseBox", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", boxId.ToString() },
            { "boxBarcode", barcode },
            { "reason", "Fin de poste - Clôture anticipée" }
        }));
        Assert.Equal(HttpStatusCode.Redirect, forceClose.StatusCode);

        // 9. Fetch box state and verify status and exception reason
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var box = await db.Boxes.Include(b => b.Packages).FirstOrDefaultAsync(b => b.Id == boxId);
            Assert.NotNull(box);
            Assert.Equal(BoxStatus.CompletedWithException, box.Status);
            Assert.Equal("Fin de poste - Clôture anticipée", box.ExceptionReason);

            // 10. Retrieve the BoxAuditLogs. Assert that all entries exist, and verify DetailsJson
            // Since package transfer logs are associated with the destination box ID, we fetch all audit logs for either boxId or destBoxId.
            var logs = await db.BoxAuditLogs
                .Where(l => l.BoxId == boxId || l.BoxId == destBoxId)
                .ToListAsync();
            Assert.NotEmpty(logs);

            // Verified action types generated by the interceptor during the flow
            Assert.Contains(logs, l => l.ActionType == "Insert" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "Update" && l.BoxId == boxId);
            Assert.Contains(logs, l => l.ActionType == "PackageScan" && l.BoxId == boxId);
            
            // Verify transfer operation audit log
            // BoxPackage update log has BoxId = destBoxId (since BoxId changed to destBoxId)
            Assert.Contains(logs, l => l.ActionType == "Update" && l.BoxId == destBoxId && l.DetailsJson?.Contains("BoxPackage") == true);

            // Verify details in DetailsJson
            var insertLog = logs.First(l => l.ActionType == "Insert" && l.BoxId == boxId);
            Assert.Contains("ExpectedQuantity", insertLog.DetailsJson ?? "");

            var scanLog = logs.First(l => l.ActionType == "PackageScan" && l.BoxId == boxId);
            Assert.Contains("PKG-E2E-001", scanLog.DetailsJson ?? "");

            var transferLog = logs.First(l => l.ActionType == "Update" && l.BoxId == destBoxId && l.DetailsJson?.Contains("BoxPackage") == true);
            Assert.Contains("BoxId", transferLog.DetailsJson ?? "");
            
            // Verify that the supervisor reason is captured in the Box entities updates log
            var sourceUpdateLogs = logs.Where(l => l.ActionType == "Update" && l.BoxId == boxId).ToList();
            Assert.Contains(sourceUpdateLogs, l => l.DetailsJson?.Contains("Transfer de package E2E") == true);
        }
    }
}
