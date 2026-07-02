using System.Net;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.Contains("déjà", decodedContent, StringComparison.OrdinalIgnoreCase);
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

        var detailsResponse = await client.GetAsync(detailsUrl);
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
        Assert.Contains("Le code-barres doit contenir au moins 3 caractères.", decodedContent);
    }
}
