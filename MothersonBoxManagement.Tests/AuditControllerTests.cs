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
using MothersonBoxManagement.Entities;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class AuditControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AccessIndex_AsAnonymous_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Audit");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task AccessIndex_AsOperator_RedirectsToLoginDueToAccessDenied()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "OP001", "Motherson2026!");

        // Act
        var response = await client.GetAsync("/Audit");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task AccessIndex_AsSupervisor_ReturnsSuccessAndView()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");

        // Act
        var response = await client.GetAsync("/Audit");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Audit Log", content);
    }

    [Fact]
    public async Task AccessIndex_FilterByBoxId_AppliesFilter()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var operatorUser = await db.Users.FirstAsync(u => u.Matricule == "OP001");

            // Seed a box and associated audit logs
            var box = new Box
            {
                BoxNumber = "BOX-AUDIT-FILTER",
                BarcodeValue = "BOX-AUDIT-FILTER",
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 5,
                Status = BoxStatus.Open,
                CreatedByUserId = operatorUser.Id,
                CreatedAt = DateTime.Now
            };
            db.Boxes.Add(box);
            await db.SaveChangesAsync();

            var auditLog = new BoxAuditLog
            {
                BoxId = box.Id,
                UserId = operatorUser.Id,
                ActionType = "TestFilterBoxId",
                Timestamp = DateTime.Now,
                DetailsJson = "{}"
            };
            db.BoxAuditLogs.Add(auditLog);
            await db.SaveChangesAsync();

            // Act
            var response = await client.GetAsync($"/Audit?boxId={box.Id}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("TestFilterBoxId", content);
        }
    }

    [Fact]
    public async Task AccessIndex_FilterByActionType_AppliesFilter()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var operatorUser = await db.Users.FirstAsync(u => u.Matricule == "OP001");

            var box = new Box
            {
                BoxNumber = "BOX-AUDIT-TYPE",
                BarcodeValue = "BOX-AUDIT-TYPE",
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 5,
                Status = BoxStatus.Open,
                CreatedByUserId = operatorUser.Id,
                CreatedAt = DateTime.Now
            };
            db.Boxes.Add(box);
            await db.SaveChangesAsync();

            var auditLog = new BoxAuditLog
            {
                BoxId = box.Id,
                UserId = operatorUser.Id,
                ActionType = "UniqueActionTypeFilter",
                Timestamp = DateTime.Now,
                DetailsJson = "{}"
            };
            db.BoxAuditLogs.Add(auditLog);
            await db.SaveChangesAsync();

            // Act
            var response = await client.GetAsync($"/Audit?actionType=UniqueActionTypeFilter");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("UniqueActionTypeFilter", content);
        }
    }

    [Fact]
    public async Task AccessIndex_FilterByDateRange_AppliesFilter()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var operatorUser = await db.Users.FirstAsync(u => u.Matricule == "OP001");

            var box = new Box
            {
                BoxNumber = "BOX-AUDIT-DATE",
                BarcodeValue = "BOX-AUDIT-DATE",
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 5,
                Status = BoxStatus.Open,
                CreatedByUserId = operatorUser.Id,
                CreatedAt = DateTime.Now
            };
            db.Boxes.Add(box);
            await db.SaveChangesAsync();

            // Create one log in range, one out of range
            var auditLogToday = new BoxAuditLog
            {
                BoxId = box.Id,
                UserId = operatorUser.Id,
                ActionType = "DateFilterToday",
                Timestamp = DateTime.Now,
                DetailsJson = "{}"
            };
            var auditLogOld = new BoxAuditLog
            {
                BoxId = box.Id,
                UserId = operatorUser.Id,
                ActionType = "DateFilterOld",
                Timestamp = DateTime.Now.AddDays(-10),
                DetailsJson = "{}"
            };
            db.BoxAuditLogs.AddRange(auditLogToday, auditLogOld);
            await db.SaveChangesAsync();

            // Act - query date range containing only today
            var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            var response = await client.GetAsync($"/Audit?fromDate={todayStr}&toDate={todayStr}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("DateFilterToday", content);
            Assert.DoesNotContain("badge bg-info text-dark\">DateFilterOld", content);
        }
    }

    [Fact]
    public async Task AccessIndex_Pagination_PaginatesCorrectly()
    {
        // Arrange
        var client = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var operatorUser = await db.Users.FirstAsync(u => u.Matricule == "OP001");

            var box = new Box
            {
                BoxNumber = "BOX-AUDIT-PAGINATION",
                BarcodeValue = "BOX-AUDIT-PAGINATION",
                Type = BoxType.Carton,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 5,
                Status = BoxStatus.Open,
                CreatedByUserId = operatorUser.Id,
                CreatedAt = DateTime.Now
            };
            db.Boxes.Add(box);
            await db.SaveChangesAsync();

            // Insert 25 logs
            var logs = new List<BoxAuditLog>();
            for (int i = 1; i <= 25; i++)
            {
                logs.Add(new BoxAuditLog
                {
                    BoxId = box.Id,
                    UserId = operatorUser.Id,
                    ActionType = $"LogItem-{i}",
                    Timestamp = DateTime.Now.AddSeconds(-i),
                    DetailsJson = "{}"
                });
            }
            db.BoxAuditLogs.AddRange(logs);
            await db.SaveChangesAsync();

            // Act - Page 1
            var response1 = await client.GetAsync("/Audit?page=1&pageSize=20");
            var content1 = await response1.Content.ReadAsStringAsync();

            // Act - Page 2
            var response2 = await client.GetAsync("/Audit?page=2&pageSize=20");
            var content2 = await response2.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            
            // Verify pagination navigation links are generated correctly
            Assert.Contains("page=2", content1);
            Assert.Contains("page=1", content2);
        }
    }
}
