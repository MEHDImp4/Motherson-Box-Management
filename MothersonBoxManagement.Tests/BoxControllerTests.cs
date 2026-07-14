using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
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

    private async Task<int> CreateTemplateAsync(HttpClient client)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", "Test Template " + Guid.NewGuid().ToString("N")[..6]),
            new KeyValuePair<string, string>("Type", "Cardboard"),
            new KeyValuePair<string, string>("Height", "30"),
            new KeyValuePair<string, string>("Width", "20"),
            new KeyValuePair<string, string>("Depth", "15"),
            new KeyValuePair<string, string>("ExpectedQuantity", "50")
        });

        var response = await client.PostAsync("/BoxTemplate/Create", formData);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var template = await db.BoxTemplates.OrderByDescending(t => t.Id).FirstAsync();
        return template.Id;
    }

    private async Task<string> CreateBoxFromTemplateAsync(HttpClient client, int templateId)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("templateId", templateId.ToString())
        });

        var response = await client.PostAsync("/Box/CreateFromTemplate", formData);

        var location = response.Headers.Location?.OriginalString!;
        return location.Split('/').Last();
    }

    [Fact]
    public async Task CreateFromTemplate_ReturnsRedirectToDetails()
    {
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("templateId", templateId.ToString())
        });

        var response = await client.PostAsync("/Box/CreateFromTemplate", formData);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Box/Details/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateFromTemplate_Operator_ReturnsRedirectToDetails()
    {
        var supervisorClient = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(supervisorClient);
        var operatorClient = await LoginAsync("OP001");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("templateId", templateId.ToString())
        });

        var response = await operatorClient.PostAsync("/Box/CreateFromTemplate", formData);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Box/PrintClient/", response.Headers.Location?.OriginalString);
        Assert.Contains("autoPrint=True", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BoxTemplate_Index_ReturnsTemplates()
    {
        var client = await LoginAsync("SP001");

        var response = await client.GetAsync("/BoxTemplate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Box Templates", content);
    }

    [Fact]
    public async Task BoxDetails_ExistingBox_ReturnsDetails()
    {
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);
        var boxBarcode = await CreateBoxFromTemplateAsync(client, templateId);

        var response = await client.GetAsync($"/Box/Details/{boxBarcode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("BOX-", content);
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
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);
        await CreateBoxFromTemplateAsync(client, templateId);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome", content);
    }

    [Fact]
    public async Task BoxCreation_GeneratesValidBoxNumber()
    {
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);
        var boxBarcode = await CreateBoxFromTemplateAsync(client, templateId);

        var response = await client.GetAsync($"/Box/Details/{boxBarcode}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("BOX-", content);
    }

    [Fact]
    public async Task Homepage_DoesNotAutofocusScannerInput()
    {
        var client = await LoginAsync();
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("autofocus", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TemplateSelectionPage_ContainsCreateFromTemplateForms()
    {
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);

        var response = await client.GetAsync("/Dashboard/Templates");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/Box/CreateFromTemplate", content);
        Assert.Contains($"value=\"{templateId}\"", content);
    }

    [Fact]
    public async Task Homepage_Lookup_OpenBox_RedirectsToDetails()
    {
        var client = await LoginAsync("SP001");
        var templateId = await CreateTemplateAsync(client);
        var boxBarcode = await CreateBoxFromTemplateAsync(client, templateId);
        
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
        Assert.Contains("value=\"PKG-123456\"", content);
        Assert.Contains("alert-warning", content);
    }

    [Fact]
    public async Task BoxSearchPage_OpenNewBoxLink_TargetsDedicatedTemplateSelectionPage()
    {
        var client = await LoginAsync();

        var response = await client.GetAsync("/Box");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/Dashboard/Templates", content);
        Assert.DoesNotContain("showTemplatePicker", content);
    }
}
