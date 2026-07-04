using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class ConcurrencySimulatingInterceptor : SaveChangesInterceptor
{
    public int FailuresCount { get; set; } = 0;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (FailuresCount > 0)
        {
            FailuresCount--;
            
            if (eventData.Context is not null)
            {
                // Detach added packages to prevent duplicate insertions in InMemory retry loop
                var addedPackages = eventData.Context.ChangeTracker.Entries<BoxPackage>()
                    .Where(e => e.State == EntityState.Added)
                    .ToList();
                foreach (var entry in addedPackages)
                {
                    entry.State = EntityState.Detached;
                }
            }
            
            throw new DbUpdateConcurrencyException("Simulated optimistic concurrency exception.");
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

public class UniqueConstraintSimulatingInterceptor : SaveChangesInterceptor
{
    private readonly HashSet<string> _seenBarcodes = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var newPackages = eventData.Context.ChangeTracker.Entries<BoxPackage>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var pkg in newPackages)
        {
            lock (_seenBarcodes)
            {
                if (_seenBarcodes.Contains(pkg.PackageBarcode))
                {
                    var inner = new Exception("IX_BoxPackages_PackageBarcode");
                    throw new DbUpdateException("Duplicate package barcode unique index violation.", inner);
                }
                _seenBarcodes.Add(pkg.PackageBarcode);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

public class ConcurrencyTests
{
    [Fact]
    public async Task ScanPackage_RetryOnConcurrencyConflict_Succeeds()
    {
        // Arrange
        var dbName = "ConcurrencyRetryDb_" + Guid.NewGuid();
        var interceptor = new ConcurrencySimulatingInterceptor();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName);
            options.AddInterceptors(interceptor);
        });
        
        services.AddScoped<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.EnsureCreatedAsync();

        var user = new User { Matricule = "OP001", Role = "Operator", IsActive = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var box = new Box
        {
            BoxNumber = "BOX-CONCURRENCY-001",
            BarcodeValue = "BOX-CONCURRENCY-001",
            Type = BoxType.Carton,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.Now
        };
        db.Boxes.Add(box);
        await db.SaveChangesAsync();

        var boxService = new BoxService(db);

        // Activate concurrency exception simulation for 1 save
        interceptor.FailuresCount = 1;

        // Act
        var result = await boxService.ScanPackageAsync(box.Id, "PKG-CONC-001", user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("1/5 paquets", result.Message);
        
        var updatedBox = await db.Boxes.Include(b => b.Packages).FirstOrDefaultAsync(b => b.Id == box.Id);
        Assert.NotNull(updatedBox);
        Assert.Equal(1, updatedBox.CurrentQuantity);
        Assert.Single(updatedBox.Packages);
        Assert.Equal("PKG-CONC-001", updatedBox.Packages.First().PackageBarcode);
    }

    [Fact]
    public async Task ScanPackage_DuplicateBarcodeConcurrent_ExactlyOneSucceeds()
    {
        // Arrange
        var dbName = "ConcurrencyDuplicateDb_" + Guid.NewGuid();
        var interceptor = new UniqueConstraintSimulatingInterceptor();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(dbName);
            options.AddInterceptors(interceptor);
        });
        services.AddScoped<MothersonBoxManagement.Data.Interceptors.AuditSaveChangesInterceptor>();

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.EnsureCreatedAsync();

        var user = new User { Matricule = "OP001", Role = "Operator", IsActive = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var box = new Box
        {
            BoxNumber = "BOX-CONCURRENCY-002",
            BarcodeValue = "BOX-CONCURRENCY-002",
            Type = BoxType.Carton,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.Now
        };
        db.Boxes.Add(box);
        await db.SaveChangesAsync();

        var boxService = new BoxService(db);
        
        const string duplicateBarcode = "PKG-CONC-DUPLICATE";
        
        // Act - Simulate parallel scan tasks
        int taskCount = 8;
        var tasks = new List<Task<ScanResult>>();
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Each task needs its own DbContext and service instance to run concurrently
                using var taskScope = serviceProvider.CreateScope();
                var taskDb = taskScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var taskBoxService = new BoxService(taskDb);
                return await taskBoxService.ScanPackageAsync(box.Id, duplicateBarcode, user.Id);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulScans = results.Where(r => r.Success).ToList();
        var failedScans = results.Where(r => !r.Success).ToList();

        Assert.Single(successfulScans);
        Assert.Equal(taskCount - 1, failedScans.Count);

        foreach (var failure in failedScans)
        {
            Assert.Contains("Ce code-barres paquet a déjà été scanné.", failure.Message);
        }
    }
}
