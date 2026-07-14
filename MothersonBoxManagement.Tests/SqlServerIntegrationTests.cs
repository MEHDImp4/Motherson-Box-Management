using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using Xunit;

namespace MothersonBoxManagement.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOTHERSON_TEST_SQL_CONNECTION")))
            Skip = "Set MOTHERSON_TEST_SQL_CONNECTION to run SQL Server integration tests.";
    }
}

public class SqlServerIntegrationTests
{
    private static string CreateDatabaseConnectionString()
    {
        var baseConnection = Environment.GetEnvironmentVariable("MOTHERSON_TEST_SQL_CONNECTION")
            ?? throw new InvalidOperationException("MOTHERSON_TEST_SQL_CONNECTION is required.");
        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = "MothersonIntegration_" + Guid.NewGuid().ToString("N")
        };
        return builder.ConnectionString;
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    [SqlServerFact]
    public async Task MigrationsAndCriticalConstraints_WorkOnRealSqlServer()
    {
        var connectionString = CreateDatabaseConnectionString();
        await using var db = CreateContext(connectionString);
        try
        {
            await db.Database.MigrateAsync();

            var constraintCount = await db.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM sys.check_constraints WHERE [name] IN ('CK_Boxes_ExpectedQuantity_Positive','CK_Boxes_CurrentQuantity_Range','CK_Boxes_Dimensions_Positive')")
                .SingleAsync();
            Assert.Equal(3, constraintCount);

            var systemUser = await db.Users.SingleAsync(user => user.Matricule == "SYSTEM");
            db.Boxes.Add(new Box
            {
                BoxNumber = "BOX-INVALID-SQL",
                BarcodeValue = "BOX-INVALID-SQL",
                Type = BoxType.Cardboard,
                Height = 0,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 0,
                CurrentQuantity = 1,
                Status = BoxStatus.Open,
                CreatedByUserId = systemUser.Id,
                CreatedAt = DateTime.UtcNow
            });

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(547, Assert.IsType<SqlException>(exception.InnerException).Number);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task ConcurrentDuplicateScan_ExactlyOneAssociationIsCommitted()
    {
        var connectionString = CreateDatabaseConnectionString();
        await using var setup = CreateContext(connectionString);
        try
        {
            await setup.Database.MigrateAsync();
            var user = new User
            {
                Matricule = "SQL001",
                FullName = "SQL Integration Operator",
                PasswordHash = "LOGIN-DISABLED",
                Role = "Operator",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            setup.Users.Add(user);
            await setup.SaveChangesAsync();

            var barcodeService = new BarcodeService(setup);
            var boxService = new BoxService(setup, barcodeService);
            var box = await boxService.CreateBoxAsync(new CreateBoxDto
            {
                Type = BoxType.Cardboard,
                Height = 10,
                Width = 10,
                Depth = 10,
                ExpectedQuantity = 2
            }, user.Id);
            await boxService.OpenBoxAsync(box.Id, user.Id, "SQL-SETUP");

            async Task<ScanResult> ScanAsync(string station)
            {
                await using var context = CreateContext(connectionString);
                var barcode = new BarcodeService(context);
                var boxes = new BoxService(context, barcode);
                var scanner = new PackageScanService(context, barcode, new AuditService(context), boxes);
                return await scanner.ScanPackageAsync(box.Id, "PKG-SQL-RACE-001", user.Id, station);
            }

            var results = await Task.WhenAll(ScanAsync("SQL-A"), ScanAsync("SQL-B"));
            Assert.Single(results, result => result.Success);
            Assert.Single(results, result => !result.Success);

            setup.ChangeTracker.Clear();
            Assert.Equal(1, await setup.BoxPackages.CountAsync(package =>
                package.PackageBarcode == "PKG-SQL-RACE-001" && !package.IsRemoved));
            Assert.Equal(1, (await setup.Boxes.SingleAsync(candidate => candidate.Id == box.Id)).CurrentQuantity);
        }
        finally
        {
            await setup.Database.EnsureDeletedAsync();
        }
    }
}
