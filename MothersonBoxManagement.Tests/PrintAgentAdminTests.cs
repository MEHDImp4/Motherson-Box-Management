using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;

namespace MothersonBoxManagement.Tests;

public class PrintAgentAdminTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public PrintAgentAdminTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Administrator_CanConfigurePrinter_AndGeneratePairingCode()
    {
        var station = $"P3-{Guid.NewGuid():N}"[..16];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.PrinterConfigurations.Add(new PrinterConfiguration
            {
                Code = station,
                PcName = station,
                AvailablePrintersJson = "[\"ZDesigner ZT411\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");

        var configure = await client.PostAsync("/PrintAgent/Configure", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workstationName"] = station,
            ["printerName"] = "ZDesigner ZT411",
            ["printMode"] = PrintModes.Zpl
        }));
        var pairing = await client.PostAsync("/PrintAgent/PairingCode", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workstationName"] = station
        }));
        var pairingBody = await pairing.Content.ReadFromJsonAsync<PairingCodeResponse>();

        Assert.Equal(HttpStatusCode.Redirect, configure.StatusCode);
        Assert.Equal(HttpStatusCode.OK, pairing.StatusCode);
        Assert.NotNull(pairingBody);
        Assert.Equal(12, pairingBody!.Code.Length);
    }

    [Fact]
    public async Task Operator_CannotGeneratePairingCode()
    {
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var response = await client.PostAsync("/PrintAgent/PairingCode", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workstationName"] = "P3-OPERATOR"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotDownloadAgent()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/Downloads/PrintAgent");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Settings_HidesAgentInstallationFromOperator_AndShowsItToSupervisorAndAdministrator()
    {
        var operatorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var supervisorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var operatorHtml = await (await operatorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();
        var supervisorHtml = await (await supervisorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();
        var administratorHtml = await (await administratorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("Download print agent", operatorHtml);
        Assert.DoesNotContain("Generate pairing code", operatorHtml);
        Assert.Contains("Download print agent", supervisorHtml);
        Assert.Contains("Generate pairing code", supervisorHtml);
        Assert.DoesNotContain("id=\"fleetRows\"", supervisorHtml);
        Assert.Contains("Generate pairing code", administratorHtml);
        Assert.Contains("Complete print history", administratorHtml);
        Assert.DoesNotContain("id=\"fleetRows\"", administratorHtml);
    }

    [Fact]
    public async Task ManualQueue_CreatesPendingJobForCurrentWorkstation()
    {
        var station = $"Q-{Guid.NewGuid():N}"[..16];
        string barcode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(candidate => candidate.Matricule == "OP001");
            var box = new Box
            {
                BoxNumber = $"BOX-{Guid.NewGuid():N}"[..20],
                BarcodeValue = $"BOX-{Guid.NewGuid():N}"[..20],
                Type = BoxType.Cardboard,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 1,
                Status = BoxStatus.Open,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.AddRange(box, new PrinterConfiguration
            {
                Code = station,
                PcName = station,
                PrinterName = "ZDesigner ZT411",
                PrintMode = PrintModes.Zpl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            barcode = box.BarcodeValue;
        }
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var response = await client.PostAsync($"/Print/Queue/{barcode}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workstationName"] = station
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Contains(await verifyDb.BoxPrintJobs.ToListAsync(), job => job.Status == PrintJobStatuses.Pending && job.PrinterName == "ZDesigner ZT411");
    }

    [Fact]
    public async Task Status_ShowsPendingLabels_WhenTheWorkstationIsOffline()
    {
        var station = $"QUEUE-{Guid.NewGuid():N}"[..16];
        string boxNumber;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(candidate => candidate.Matricule == "OP001");
            var workstation = new PrinterConfiguration
            {
                Code = station,
                PcName = station,
                PrinterName = "ZDesigner ZT411",
                PrintMode = PrintModes.Zpl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var box = new Box
            {
                BoxNumber = $"BOX-{Guid.NewGuid():N}"[..20],
                BarcodeValue = $"BOX-{Guid.NewGuid():N}"[..20],
                Type = BoxType.Cardboard,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 1,
                Status = BoxStatus.Open,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.AddRange(workstation, box);
            await db.SaveChangesAsync();
            boxNumber = box.BoxNumber;
            var service = scope.ServiceProvider.GetRequiredService<IPrintAgentService>();
            await service.QueueAsync(box.Id, user.Id, workstation.Id);
        }

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var response = await client.GetAsync($"/PrintAgent/Status?workstationName={station}");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, payload.RootElement.GetProperty("queue").GetProperty("pending").GetInt32());
        Assert.Equal(boxNumber, payload.RootElement.GetProperty("recentJobs")[0].GetProperty("boxNumber").GetString());
    }

    [Fact]
    public async Task Administrator_CanSeeFleetStatus_WhileOperatorCannot()
    {
        var station = $"FLEET-{Guid.NewGuid():N}"[..16];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(candidate => candidate.Matricule == "OP001");
            var workstation = new PrinterConfiguration
            {
                Code = station,
                PcName = station,
                MachineName = "Factory-PC-07",
                PrinterName = "ZDesigner ZT411",
                AgentVersion = "1.2.3",
                LastSeenAt = DateTime.UtcNow,
                LastIpAddress = "10.20.30.40",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Add(workstation);
            db.Add(new BoxAuditLog
            {
                WorkstationName = station,
                ActionType = "BOX_SCANNED",
                UserId = user.Id,
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var adminResponse = await administratorClient.GetAsync("/PrintAgent/Fleet");
        using var payload = JsonDocument.Parse(await adminResponse.Content.ReadAsStringAsync());
        var fleetStation = payload.RootElement.EnumerateArray().Single(item => item.GetProperty("code").GetString() == station);

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal("Factory-PC-07", fleetStation.GetProperty("machineName").GetString());
        Assert.Equal("ZDesigner ZT411", fleetStation.GetProperty("printerName").GetString());
        Assert.True(fleetStation.GetProperty("isOnline").GetBoolean());
        Assert.Equal("10.20.30.40", fleetStation.GetProperty("lastIpAddress").GetString());
        Assert.Equal("BOX_SCANNED", fleetStation.GetProperty("lastAction").GetString());
        Assert.Equal("Test Operator", fleetStation.GetProperty("lastUser").GetString());

        var operatorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var operatorResponse = await operatorClient.GetAsync("/PrintAgent/Fleet");
        Assert.Equal(HttpStatusCode.Redirect, operatorResponse.StatusCode);
    }

    [Fact]
    public async Task WorkstationSupervisionPage_IsReservedForAdministrators()
    {
        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var operatorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        var administratorResponse = await administratorClient.GetAsync("/PrintAgent/Workstations");
        var operatorResponse = await operatorClient.GetAsync("/PrintAgent/Workstations");

        Assert.Equal(HttpStatusCode.OK, administratorResponse.StatusCode);
        Assert.Contains("Workstation supervision", await administratorResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, operatorResponse.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanRemotelyConfigureAWorkstation()
    {
        int workstationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var workstation = new PrinterConfiguration
            {
                Code = $"REMOTE-{Guid.NewGuid():N}"[..16],
                PcName = "Factory-PC-09",
                AvailablePrintersJson = "[\"ZDesigner ZT411\",\"Office Printer\"]",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Add(workstation);
            await db.SaveChangesAsync();
            workstationId = workstation.Id;
        }

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var response = await client.PostAsync($"/PrintAgent/Workstations/{workstationId}/Configure", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Packaging station 9",
            ["printerName"] = "ZDesigner ZT411",
            ["printMode"] = PrintModes.Zpl
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await verifyDb.PrinterConfigurations.FindAsync(workstationId);
        Assert.NotNull(saved);
        Assert.Equal("Packaging station 9", saved!.DisplayName);
        Assert.Equal("ZDesigner ZT411", saved.PrinterName);
        Assert.Equal(PrintModes.Zpl, saved.PrintMode);
    }

    public sealed record PairingCodeResponse(string Code, DateTime ExpiresAt);
}
