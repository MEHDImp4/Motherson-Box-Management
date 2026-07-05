using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class ScanControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ScanControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, string DetailsUrl)> CreateAndOpenBox(HttpClient client)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "10"),
            new KeyValuePair<string, string>("ExpectedQuantity", "3")
        });

        var response = await client.PostAsync("/Box/Create", formData);
        return (client, response.Headers.Location?.OriginalString)!;
    }

    private async Task<HttpClient> LoginAsync()
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

    [Fact]
    public async Task ScanValidPackage_Success()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "PKG-001")
        });

        var response = await client.PostAsync("/Box/Scan", scanForm);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var detailsResponse = await client.GetAsync(detailsUrl);
        var content = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("PKG-001", content);
    }

    [Fact]
    public async Task ScanDuplicatePackage_Rejected()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scan = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "PKG-DUP-001")
        });
        await client.PostAsync("/Box/Scan", scan);

        var scan2 = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "PKG-DUP-001")
        });
        var response2 = await client.PostAsync("/Box/Scan", scan2);
        var redirectUrl = response2.Headers.Location?.OriginalString;

        var details2 = await client.GetAsync(redirectUrl!);
        var content = await details2.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        Assert.Contains("already", decodedContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanBoxBarcode_Rejected()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "BOX-SOMETHING")
        });

        var response = await client.PostAsync("/Box/Scan", scanForm);
        var redirectUrl = response.Headers.Location?.OriginalString;

        var detailsResponse = await client.GetAsync(redirectUrl!);
        var content = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("box", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanPackage_AutoCompletesBox()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        for (int i = 1; i <= 3; i++)
        {
            var scanForm = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
                new KeyValuePair<string, string>("boxBarcode", boxBarcode),
                new KeyValuePair<string, string>("barcode", $"PKG-AUTO-{i}")
            });
            await client.PostAsync("/Box/Scan", scanForm);
        }

        var detailsResponse = await client.GetAsync($"/Box/Details/{boxBarcode}");
        var content = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Completed", content);
    }

    [Fact]
    public async Task ScanPackage_EmptyBarcode_ReturnsToDetails()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "")
        });

        var response = await client.PostAsync("/Box/Scan", scanForm);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task ScanPackage_TooShortBarcode_ReturnsError()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "12")
        });

        var response = await client.PostAsync("/Box/Scan", scanForm);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        var redirectUrl = response.Headers.Location?.OriginalString;
        var detailsResponse = await client.GetAsync(redirectUrl!);
        var content = await detailsResponse.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        Assert.Contains("The barcode must contain at least 3 characters.", decodedContent);
    }

    [Fact]
    public async Task ScanOnCompletedBox_Rejected()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var testRunId = Guid.NewGuid().ToString("N")[..6];

        // Scan 3 packages to auto-complete the box (ExpectedQuantity is 3)
        for (int i = 1; i <= 3; i++)
        {
            var scanForm = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
                new KeyValuePair<string, string>("boxBarcode", boxBarcode),
                new KeyValuePair<string, string>("barcode", $"PKG-AUTO-{testRunId}-{i}")
            });
            await client.PostAsync("/Box/Scan", scanForm);
        }

        // Try to scan a 4th package
        var scan4 = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", $"PKG-EXTRA-{testRunId}")
        });
        var response = await client.PostAsync("/Box/Scan", scan4);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Follow the first redirect: from Scan to Prepare (which redirects again to Details)
        var redirectUrl1 = response.Headers.Location?.OriginalString;
        var responsePrepare = await client.GetAsync(redirectUrl1!);
        Assert.Equal(HttpStatusCode.Redirect, responsePrepare.StatusCode);

        // Follow the second redirect: from Prepare to Details
        var redirectUrl2 = responsePrepare.Headers.Location?.OriginalString;
        var detailsResponse = await client.GetAsync(redirectUrl2!);
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

        var content = await detailsResponse.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        Assert.Contains("not open for scanning", decodedContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanOnNonExistentBox_ReturnsError()
    {
        var client = await LoginAsync();

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", "99999"),
            new KeyValuePair<string, string>("boxBarcode", "BOX-FAKE"),
            new KeyValuePair<string, string>("barcode", "PKG-TEST-123")
        });

        var response = await client.PostAsync("/Box/Scan", scanForm);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var redirectUrl = response.Headers.Location?.OriginalString;
        var detailsResponse = await client.GetAsync(redirectUrl!);
        Assert.Equal(HttpStatusCode.NotFound, detailsResponse.StatusCode);
    }

    [Fact]
    public async Task ScanAjax_ReturnsJsonOnSuccess()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var barcode = $"PKG-AJAX-{Guid.NewGuid():N}";
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", barcode)
        });

        var response = await client.PostAsync("/Box/ScanAjax", scanForm);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var successVal = jsonDoc.RootElement.GetProperty("success").GetBoolean();
        var messageVal = jsonDoc.RootElement.GetProperty("message").GetString();

        Assert.True(successVal);
        Assert.Contains("Scan successful", messageVal);
    }

    [Fact]
    public async Task ScanAjax_AuditLogCreated()
    {
        var client = await LoginAsync();
        var (_, detailsUrl) = await CreateAndOpenBox(client);
        var boxBarcode = detailsUrl.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var barcode = $"PKG-AJAX-AUDIT-{Guid.NewGuid():N}";
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", barcode)
        });

        var response = await client.PostAsync("/Box/ScanAjax", scanForm);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var log = await db.BoxAuditLogs
            .FirstOrDefaultAsync(al => al.BoxId == box.Id && al.ActionType == "PackageScanned");

        Assert.NotNull(log);
        Assert.Contains(barcode, log.DetailsJson);
    }

    [Fact]
    public async Task ScanOnCapacityReachedBox_Rejected()
    {
        var client = await LoginAsync();
        
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "10"),
            new KeyValuePair<string, string>("ExpectedQuantity", "2")
        });

        var responseCreate = await client.PostAsync("/Box/Create", formData);
        var detailsUrl = responseCreate.Headers.Location?.OriginalString;
        var boxBarcode = detailsUrl!.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var testRunId = Guid.NewGuid().ToString("N")[..6];

        for (int i = 1; i <= 2; i++)
        {
            var scanForm = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
                new KeyValuePair<string, string>("boxBarcode", boxBarcode),
                new KeyValuePair<string, string>("barcode", $"PKG-CAP-{testRunId}-{i}")
            });
            await client.PostAsync("/Box/Scan", scanForm);
        }

        var scan3 = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", $"PKG-CAP-EXTRA-{testRunId}")
        });
        var response3 = await client.PostAsync("/Box/Scan", scan3);

        Assert.Equal(HttpStatusCode.Redirect, response3.StatusCode);

        // Follow the first redirect: from Scan to Prepare (which redirects again to Details)
        var redirectUrl1 = response3.Headers.Location?.OriginalString;
        var responsePrepare = await client.GetAsync(redirectUrl1!);
        Assert.Equal(HttpStatusCode.Redirect, responsePrepare.StatusCode);

        // Follow the second redirect: from Prepare to Details
        var redirectUrl2 = responsePrepare.Headers.Location?.OriginalString;
        var detailsResponse = await client.GetAsync(redirectUrl2!);
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);

        var content = await detailsResponse.Content.ReadAsStringAsync();
        var decodedContent = System.Net.WebUtility.HtmlDecode(content);
        Assert.Contains("not open for scanning", decodedContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScanConcurrentSamePackage_ModelHasUniqueIndex()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var model = db.Model.FindEntityType(typeof(BoxPackage));
        var index = model?.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(BoxPackage.PackageBarcode)));
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }
}
