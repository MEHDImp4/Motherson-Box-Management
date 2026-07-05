using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class DashboardControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_AnonymousUser_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Index_AuthenticatedUser_DisplaysOpenBoxesAndUserInfo()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OP001", content);
        Assert.Contains("Welcome", content);
    }

    [Fact]
    public async Task SearchBarcode_Empty_ReturnsValidationError()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", "")
        });

        // Act
        var response = await client.PostAsync("/", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please enter a barcode.", content);
    }

    [Fact]
    public async Task SearchBarcode_IsPackageBarcode_ReturnsFormatWarning()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", "PKG-12345")
        });

        // Act
        var response = await client.PostAsync("/", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("value=\"PKG-12345\"", content);
        Assert.Contains("alert-warning", content);
    }

    [Fact]
    public async Task SearchBarcode_NonExistentBox_ReturnsNotFoundError()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", "BOX-NOT-REAL-999")
        });

        // Act
        var response = await client.PostAsync("/", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("value=\"BOX-NOT-REAL-999\"", content);
        Assert.Contains("alert-danger", content);
    }

    [Fact]
    public async Task SearchBarcode_OpenBox_RedirectsToPrepare()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        
        string barcode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(u => u.Matricule == "OP001");
            var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
            
            var boxDto = new CreateBoxDto
            {
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 5
            };
            var box = await boxService.CreateBoxAsync(boxDto, user.Id);
            barcode = box.BarcodeValue;
        }

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", barcode)
        });

        // Act
        var response = await client.PostAsync("/", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Box/Prepare/{barcode}", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task SearchBarcode_ClosedBox_RedirectsToDetails()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");
        var supervisorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        
        string barcode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FirstAsync(u => u.Matricule == "OP001");
            var spUser = await db.Users.FirstAsync(u => u.Matricule == "SP001");
            var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
            var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();
            
            var boxDto = new CreateBoxDto
            {
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 1
            };
            var box = await boxService.CreateBoxAsync(boxDto, user.Id);
            barcode = box.BarcodeValue;
            
            // Scan 1 package to auto-complete the box
            await packageScanService.ScanPackageAsync(box.Id, "PKG-COMPLETE-HOME", user.Id, "TEST-STATION");
        }

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Barcode", barcode)
        });

        // Act
        var response = await client.PostAsync("/", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Box/Details/{barcode}", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Privacy_ReturnsView()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // Act
        var response = await client.GetAsync("/Dashboard/Privacy");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_ReturnsViewAndCorrectStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Dashboard/Error?statusCode=500");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
