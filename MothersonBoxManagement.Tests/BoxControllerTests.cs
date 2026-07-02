using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class BoxControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BoxControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAsync(string matricule = "OP001", string password = "Motherson2026!")
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Matricule", matricule),
            new KeyValuePair<string, string>("Password", password)
        });

        var response = await client.PostAsync("/Account/Login", formData);

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with {response.StatusCode}: {content}");
        }

        return client;
    }

    [Fact]
    public async Task CreateBox_Get_ReturnsFormPage()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Box/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Créer une box", content);
    }

    [Fact]
    public async Task CreateBox_Post_ValidData_RedirectsToDetails()
    {
        var client = await LoginAsync();

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "50")
        });

        var response = await client.PostAsync("/Box/Create", formData);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Box/Details/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateBox_Post_InvalidDimensions_ReturnsForm()
    {
        var client = await LoginAsync();

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "0"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "50")
        });

        var response = await client.PostAsync("/Box/Create", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("sup", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BoxDetails_ExistingBox_ReturnsDetails()
    {
        var client = await LoginAsync();

        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Bois"),
            new KeyValuePair<string, string>("Height", "40"),
            new KeyValuePair<string, string>("Width", "30"),
            new KeyValuePair<string, string>("Depth", "20"),
            new KeyValuePair<string, string>("ExpectedQuantity", "100")
        });
        var createResponse = await client.PostAsync("/Box/Create", createForm);

        var location = createResponse.Headers.Location?.OriginalString!;

        var response = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BOX-", content);
        Assert.Contains("Bois", content);
    }

    [Fact]
    public async Task BoxDetails_NonexistentBox_ReturnsNotFound()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Box/Details?action=Details&id=99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Homepage_Authenticated_ShowsDashboard()
    {
        var client = await LoginAsync();

        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Plastique"),
            new KeyValuePair<string, string>("Height", "25"),
            new KeyValuePair<string, string>("Width", "25"),
            new KeyValuePair<string, string>("Depth", "25"),
            new KeyValuePair<string, string>("ExpectedQuantity", "10")
        });
        await client.PostAsync("/Box/Create", createForm);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Bienvenue", content);
        Assert.Contains("Plastique", content);
    }

    [Fact]
    public async Task BoxCreation_GeneratesValidBoxNumber()
    {
        var client = await LoginAsync();

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "20"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "20"),
            new KeyValuePair<string, string>("ExpectedQuantity", "1")
        });

        var createResponse = await client.PostAsync("/Box/Create", formData);
        var location = createResponse.Headers.Location?.OriginalString!;

        var detailsResponse = await client.GetAsync(location);
        var content = await detailsResponse.Content.ReadAsStringAsync();

        Assert.Contains("BOX-", content);
    }

    [Fact]
    public async Task BoxPrepare_OpenBox_ReturnsOk()
    {
        var client = await LoginAsync();

        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "10")
        });
        var createResponse = await client.PostAsync("/Box/Create", createForm);
        var location = createResponse.Headers.Location?.OriginalString!;
        var boxId = location.Split('/').Last();

        var response = await client.GetAsync($"/Box/Prepare/{boxId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Préparation", content);
    }

    [Fact]
    public async Task BoxPrepare_NonexistentBox_ReturnsNotFound()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Box/Prepare/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BoxPrepare_NonOpenBox_RedirectsToDetails()
    {
        var client = await LoginAsync();

        // Create box
        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "1") // quantity 1 so 1 scan completes it
        });
        var createResponse = await client.PostAsync("/Box/Create", createForm);
        var location = createResponse.Headers.Location?.OriginalString!;
        var boxBarcode = location.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        // Scan 1 item to complete the box
        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "PKG-TEST-COMPLETION")
        });
        await client.PostAsync("/Box/Scan", scanForm);

        // Prepare should redirect to Details now since box is completed (not open)
        var response = await client.GetAsync($"/Box/Prepare/{boxBarcode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Box/Details/{boxBarcode}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Homepage_Autofocus_Exists()
    {
        var client = await LoginAsync();
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("autofocus", content);
    }

    [Fact]
    public async Task Homepage_Lookup_OpenBox_RedirectsToPrepare()
    {
        var client = await LoginAsync();
        
        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "10")
        });
        var createResponse = await client.PostAsync("/Box/Create", createForm);
        var location = createResponse.Headers.Location?.OriginalString!;
        var boxBarcode = location.Split('/').Last();
        
        var lookupForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", boxBarcode)
        });
        var response = await client.PostAsync("/", lookupForm);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Box/Prepare/{boxBarcode}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Homepage_Lookup_NonOpenBox_RedirectsToDetails()
    {
        var client = await LoginAsync();
        
        var createForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Type", "Carton"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "1")
        });
        var createResponse = await client.PostAsync("/Box/Create", createForm);
        var location = createResponse.Headers.Location?.OriginalString!;
        var boxBarcode = location.Split('/').Last();

        using var scope = _factory.Services.CreateScope();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var box = await boxService.GetBoxByBarcodeAsync(boxBarcode);

        var scanForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("boxId", box!.Id.ToString()),
            new KeyValuePair<string, string>("boxBarcode", boxBarcode),
            new KeyValuePair<string, string>("barcode", "PKG-LKP-001")
        });
        await client.PostAsync("/Box/Scan", scanForm);

        var lookupForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", boxBarcode)
        });
        var response = await client.PostAsync("/", lookupForm);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Box/Details/{boxBarcode}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Homepage_Lookup_PackageBarcode_Warning()
    {
        var client = await LoginAsync();

        var lookupForm = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", "PKG-123456")
        });
        var response = await client.PostAsync("/", lookupForm);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("This is a package barcode, not a box barcode", content);
    }
}
