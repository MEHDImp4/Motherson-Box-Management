using System.Net;
using System.Net.Http.Json;
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
    public async Task Settings_ShowsDownloadToOperator_ButPairingControlsOnlyToAdministrator()
    {
        var operatorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var operatorHtml = await (await operatorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();
        var administratorHtml = await (await administratorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();

        Assert.Contains("Download print agent", operatorHtml);
        Assert.DoesNotContain("Generate pairing code", operatorHtml);
        Assert.Contains("Generate pairing code", administratorHtml);
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

    public sealed record PairingCodeResponse(string Code, DateTime ExpiresAt);
}
