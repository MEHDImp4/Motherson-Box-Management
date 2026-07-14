using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MothersonBoxManagement.Configuration;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class WebPrintingOnlyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WebPrintingOnlyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PrintClient_OffersBrowserPrintingOnly()
    {
        string barcode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(candidate => candidate.Matricule == "SP001");
            var box = new Box
            {
                BoxNumber = $"WEB-{Guid.NewGuid():N}"[..20],
                BarcodeValue = $"WEB-{Guid.NewGuid():N}"[..20],
                Type = BoxType.Cardboard,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 1,
                Status = BoxStatus.Open,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.Boxes.Add(box);
            db.PrinterConfigurations.Add(new PrinterConfiguration
            {
                Code = $"P-{Guid.NewGuid():N}"[..12],
                PrinterName = "Legacy printer",
                PrinterUncPath = @"\\legacy\printer",
                IsActive = true
            });
            await db.SaveChangesAsync();
            barcode = box.BarcodeValue;
        }

        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var response = await client.GetAsync($"/Box/PrintClient/{barcode}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("window.print()", html, StringComparison.Ordinal);
        Assert.DoesNotContain("PrintServer", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Print on Label Printer", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionConfiguration_DoesNotRequirePrintAgentSettings()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=test;User Id=test;Password=test",
            ["AllowedHosts"] = "localhost",
            ["Kestrel:Certificates:Default:Path"] = "tls.pfx",
            ["Kestrel:Certificates:Default:Password"] = "certificate-password",
            ["DataProtection:KeyPath"] = "keys",
            ["DataProtection:CertificatePath"] = "data-protection.pfx",
            ["DataProtection:CertificatePassword"] = "certificate-password"
        });

        var exception = Record.Exception(() => builder.ValidateProductionConfiguration());

        Assert.Null(exception);
    }

    [Fact]
    public void Application_DoesNotRegisterServerPrintWorkerOrSpooler()
    {
        Assert.Null(Type.GetType(
            "MothersonBoxManagement.Services.IPrintSpoolerService, MothersonBoxManagement"));
        Assert.DoesNotContain(
            _factory.Services.GetServices<IHostedService>(),
            service => service.GetType().Name == "PrintJobWorker");
    }
}
