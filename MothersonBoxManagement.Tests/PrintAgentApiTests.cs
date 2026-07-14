using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;

namespace MothersonBoxManagement.Tests;

public class PrintAgentApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PrintAgentApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Pair_Heartbeat_AndClaim_UseStationScopedBearerToken()
    {
        int workstationId;
        int boxId;
        int userId;
        string pairingCode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(candidate => candidate.Matricule == "AD001");
            var workstation = new PrinterConfiguration
            {
                Code = $"API-{Guid.NewGuid():N}"[..16],
                PcName = "P3-API",
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
            var service = scope.ServiceProvider.GetRequiredService<IPrintAgentService>();
            pairingCode = await service.CreatePairingCodeAsync(workstation.Id);
            workstationId = workstation.Id;
            boxId = box.Id;
            userId = user.Id;
        }

        var client = _factory.CreateClient();
        var pairResponse = await client.PostAsJsonAsync("/api/print-agent/pair", new
        {
            code = pairingCode,
            machineName = "PC-P3-API",
            agentVersion = "1.0.0"
        });
        var pair = await pairResponse.Content.ReadFromJsonAsync<PairAgentResult>();
        Assert.Equal(HttpStatusCode.OK, pairResponse.StatusCode);
        Assert.NotNull(pair);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pair!.Token);
        var heartbeatResponse = await client.PostAsJsonAsync("/api/print-agent/heartbeat", new
        {
            machineName = "PC-P3-API",
            agentVersion = "1.0.0",
            printers = new[] { "ZDesigner ZT411", "Microsoft Print to PDF" }
        });
        Assert.Equal(HttpStatusCode.NoContent, heartbeatResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IPrintAgentService>();
            await service.QueueAsync(boxId, userId, workstationId);
        }

        var claimResponse = await client.GetAsync("/api/print-agent/jobs/next");
        var claim = await claimResponse.Content.ReadFromJsonAsync<ClaimedPrintJob>();
        Assert.Equal(HttpStatusCode.OK, claimResponse.StatusCode);
        Assert.NotNull(claim);
        Assert.Equal(800, claim!.Payload.WidthDots);
        Assert.Equal(PrintModes.Zpl, claim.PrintMode);
    }

    [Fact]
    public async Task AgentEndpoints_RejectMissingToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/print-agent/jobs/next");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
