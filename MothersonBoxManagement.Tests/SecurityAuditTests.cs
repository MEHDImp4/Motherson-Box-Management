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
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
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
            "/Box/RetraitPackage",
            "/Box/ArchiveBox",
            "/Box/DisassociatePackage"
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
    public async Task Operator_CanCreateFromTemplate_AndConfigurePrinterSettings_ButCannotOpenOrUseSupervisorPrintActions()
    {
        var supervisorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var operatorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        var templateForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "Restricted Template " + Guid.NewGuid().ToString("N")[..6] },
            { "Type", "Cardboard" },
            { "Height", "30" },
            { "Width", "20" },
            { "Depth", "15" },
            { "ExpectedQuantity", "5" }
        });
        await supervisorClient.PostAsync("/BoxTemplate/Create", templateForm);

        int templateId;
        int createdBoxId;
        string createdBoxBarcode;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
            var user = await db.Users.FirstAsync(u => u.Matricule == "SP001");
            templateId = await db.BoxTemplates.OrderByDescending(t => t.Id).Select(t => t.Id).FirstAsync();
            var createdBox = await boxService.CreateBoxAsync(new CreateBoxDto
            {
                Type = BoxType.Cardboard,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 2
            }, user.Id);
            createdBoxId = createdBox.Id;
            createdBoxBarcode = createdBox.BarcodeValue;
        }

        var createResponse = await operatorClient.PostAsync("/Box/CreateFromTemplate", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "templateId", templateId.ToString() }
        }));
        var openResponse = await operatorClient.PostAsync("/Box/Open", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "boxId", createdBoxId.ToString() },
            { "boxBarcode", createdBoxBarcode }
        }));
        var printResponse = await operatorClient.GetAsync($"/Box/Print/{createdBoxBarcode}");
        var browserPrintResponse = await operatorClient.GetAsync($"/Box/PrintClient/{createdBoxBarcode}?autoPrint=true");
        var settingsResponse = await operatorClient.GetAsync("/Box/Settings");
        var templateSelectionResponse = await operatorClient.GetAsync("/Dashboard/Templates");
        var templateAdminResponse = await operatorClient.GetAsync("/BoxTemplate");

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.Contains("/Box/PrintClient/", createResponse.Headers.Location?.OriginalString ?? "");
        Assert.Equal(HttpStatusCode.Redirect, openResponse.StatusCode);
        Assert.Contains("/Account/Login", openResponse.Headers.Location?.OriginalString ?? "");
        Assert.Equal(HttpStatusCode.Redirect, printResponse.StatusCode);
        Assert.Contains("/Account/Login", printResponse.Headers.Location?.OriginalString ?? "");
        Assert.Equal(HttpStatusCode.OK, browserPrintResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, templateSelectionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, templateAdminResponse.StatusCode);
        Assert.Contains("/Account/Login", templateAdminResponse.Headers.Location?.OriginalString ?? "");
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
    public async Task BarcodePatterns_CanOnlyBeModifiedByAdministrator()
    {
        var supervisorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var supervisorPrefix = $"SP-{Guid.NewGuid():N}"[..10];
        var administratorPrefix = $"AD-{Guid.NewGuid():N}"[..10];

        var supervisorResponse = await supervisorClient.PostAsync("/Box/SaveBarcodeConfig", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["boxPrefix"] = supervisorPrefix,
            ["boxDatePattern"] = "yyyyMMdd",
            ["boxRandomLength"] = "6",
            ["packagePrefix"] = "PKG-",
            ["packageMinLength"] = "3"
        }));

        Assert.Equal(HttpStatusCode.Redirect, supervisorResponse.StatusCode);
        Assert.Contains("/Account/Login", supervisorResponse.Headers.Location?.OriginalString ?? "");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = await db.BarcodeConfigurations.SingleAsync(candidate => candidate.Id == 1);
            Assert.NotEqual(supervisorPrefix, config.BoxPrefix);
        }

        var administratorResponse = await administratorClient.PostAsync("/Box/SaveBarcodeConfig", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["boxPrefix"] = administratorPrefix,
            ["boxDatePattern"] = "yyyyMMdd",
            ["boxRandomLength"] = "6",
            ["packagePrefix"] = "PKG-",
            ["packageMinLength"] = "3"
        }));

        Assert.Equal(HttpStatusCode.Redirect, administratorResponse.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = await db.BarcodeConfigurations.SingleAsync(candidate => candidate.Id == 1);
            Assert.Equal(administratorPrefix, config.BoxPrefix);
        }
    }

    [Fact]
    public async Task WorkstationNameEditor_IsVisibleOnlyToAdministrator()
    {
        var supervisorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var administratorClient = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");

        var supervisorHtml = await (await supervisorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();
        var administratorHtml = await (await administratorClient.GetAsync("/Box/Settings")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("id=\"settingStation\"", supervisorHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id=\"saveSettingsBtn\"", supervisorHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"settingStation\"", administratorHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"saveSettingsBtn\"", administratorHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InputValidation_RejectsInvalidBoxDimensions()
    {
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Name", "Bad Template" },
            { "Type", "Cardboard" },
            { "Height", "-5" },
            { "Width", "0" },
            { "Depth", "10" },
            { "ExpectedQuantity", "5" }
        });

        var response = await client.PostAsync("/BoxTemplate/Create", formData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var decoded = System.Net.WebUtility.HtmlDecode(responseContent);

        Assert.Contains("Height must be at least 1 cm.", decoded);
        Assert.Contains("Width must be at least 1 cm.", decoded);
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasInvalidBox = await db.Boxes.AnyAsync(b => b.Height <= 0 || b.Width <= 0);
        Assert.False(hasInvalidBox);
    }

    [Fact]
    public async Task GlobalExceptionHandler_SuppressesDetails()
    {
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Staging");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=test;Database=test;User Id=test;Password=test;TrustServerCertificate=True");
            builder.UseSetting("AllowedHosts", "localhost");
            builder.UseSetting("Kestrel:Certificates:Default:Path", "test-certificate.pfx");
            builder.UseSetting("Kestrel:Certificates:Default:Password", "test-password");
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

    [Fact]
    public async Task HealthEndpoint_Anonymous_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task AuditWithoutHttpContext_IsAttributedToInactiveSystemPrincipal()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();

        var systemUser = await db.Users.FirstOrDefaultAsync(user => user.Matricule == "SYSTEM");
        if (systemUser is null)
        {
            systemUser = new User
            {
                Matricule = "SYSTEM",
                FullName = "Application System",
                PasswordHash = "LOGIN-DISABLED",
                Role = "System",
                IsActive = false,
                SecurityStamp = "SYSTEM-PRINCIPAL",
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(systemUser);
            await db.SaveChangesAsync();
        }

        var operatorUser = await db.Users.FirstAsync(user => user.Matricule == "OP001");
        var box = await boxService.CreateBoxAsync(new CreateBoxDto
        {
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 2
        }, operatorUser.Id);

        var audit = await db.BoxAuditLogs
            .Where(log => log.BoxId == box.Id && log.ActionType == "BoxCreated")
            .SingleAsync();

        Assert.Equal(systemUser.Id, audit.UserId);
        Assert.False(systemUser.IsActive);
        Assert.NotEqual(operatorUser.Id, audit.UserId);
    }

    [Fact]
    public async Task SystemAuditPrincipal_CannotBeModifiedOrDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var systemUser = await db.Users.SingleAsync(user => user.Matricule == "SYSTEM");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.UpdateUserAsync(systemUser.Id, "Compromised", "Administrator", true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.ResetPasswordAsync(systemUser.Id, "CompromisedPassword123!"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            userService.DeleteUserAsync(systemUser.Id));
    }

    [Fact]
    public async Task DeactivatedUserCookie_IsRejectedOnNextRequest()
    {
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var user = await db.Users.FirstAsync(u => u.Matricule == "OP001");
            await userService.DeleteUserAsync(user.Id);
        }

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? "");
    }
}
