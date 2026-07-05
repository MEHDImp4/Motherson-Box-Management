using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class ErrorTriggerStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            next(builder);

            builder.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path == "/trigger-error")
                {
                    throw new Exception("Simulated unhandled exception containing sensitive database details like: SQL Server ConnectionString=Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword; and SqlException table db.Boxes.");
                }
                await nextMiddleware();
            });
        };
    }
}

public class SecurityAuditTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityAuditTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Operator_CannotAccess_SupervisorEndpoints()
    {
        // Authenticate as Operator (OP001)
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // List of endpoints to test
        var endpoints = new[]
        {
            "/Box/CancelBox",
            "/Box/ForceCloseBox",
            "/Box/ModifyExpectedQuantity",
            "/Box/BlockBox",
            "/Box/UnblockBox",
            "/Box/BlockPackage",
            "/Box/UnblockPackage",
            "/Box/TransferPackage",
            "/Box/RetraitPackage"
        };

        foreach (var endpoint in endpoints)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "boxId", "1" },
                { "boxBarcode", "BOX-TEST" },
                { "reason", "Unauthorized action attempt" },
                { "expectedQuantity", "10" },
                { "packageId", "1" },
                { "destinationBoxId", "2" }
            });

            var response = await client.PostAsync(endpoint, content);

            // Assert they are redirected to login/access denied (default is /Account/Login)
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
        }
    }

    [Fact]
    public async Task Supervisor_CanAccess_SupervisorEndpoints()
    {
        // Authenticate as Supervisor (SP001)
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        // We will call CancelBox for a box that doesn't exist, which should fail with KeyNotFoundException
        // and redirect back to details.
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", "9999" },
            { "boxBarcode", "BOX-NOT-EXIST" },
            { "reason", "Supervisor test" }
        });

        var response = await client.PostAsync("/Box/CancelBox", content);

        // Assert it redirects to details page and NOT login/access denied
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("/Account/Login", location);
        Assert.Contains("/Box/Details", location);
    }

    [Fact]
    public async Task InputValidation_RejectsInvalidBoxDimensions()
    {
        // Authenticate as Operator (OP001)
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // POST box creation with negative/zero dimensions
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Type", "Carton" },
            { "Height", "-5" },
            { "Width", "0" },
            { "Depth", "10" },
            { "ExpectedQuantity", "5" }
        });

        var response = await client.PostAsync("/Box/Create", formData);

        // The request returns the view (HTTP 200 OK) with validation errors
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var decoded = System.Net.WebUtility.HtmlDecode(responseContent);

        Assert.Contains("Height must be greater than 0.", decoded);
        Assert.Contains("Width must be greater than 0.", decoded);
        
        // Check that no box with these invalid dimensions was created in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasInvalidBox = await db.Boxes.AnyAsync(b => b.Height <= 0 || b.Width <= 0);
        Assert.False(hasInvalidBox);
    }

    [Fact]
    public async Task InputValidation_RejectsShortBarcode()
    {
        // Authenticate as Operator (OP001)
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // POST scan with barcode of less than 3 characters
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", "1" },
            { "boxBarcode", "BOX-TEST-001" },
            { "barcode", "A" }
        });

        var response = await client.PostAsync("/Box/Scan", formData);

        // It should redirect back to Prepare page
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("/Box/Prepare", location);
        
        // Verify the AJAX endpoint
        var ajaxResponse = await client.PostAsync("/Box/ScanAjax", formData);
        Assert.Equal(HttpStatusCode.OK, ajaxResponse.StatusCode);
        var responseJson = await ajaxResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("The barcode must contain at least 3", responseJson);
    }

    [Fact]
    public async Task GlobalExceptionHandler_SuppressesDetails()
    {
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IStartupFilter, ErrorTriggerStartupFilter>();
            });
        });
        
        var client = prodFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/trigger-error");
        
        // Unhandled exceptions caught by UseExceptionHandler return 500 Internal Server Error
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        
        var htmlContent = await response.Content.ReadAsStringAsync();
        var decoded = System.Net.WebUtility.HtmlDecode(htmlContent);

        Assert.Contains("An error occurred", decoded);
        
        Assert.DoesNotContain("ConnectionString", htmlContent);
        Assert.DoesNotContain("SqlException", htmlContent);
        Assert.DoesNotContain("db.Boxes", htmlContent);
        Assert.DoesNotContain("Stack trace", htmlContent);
    }
}
