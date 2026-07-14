using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;

namespace MothersonBoxManagement.Configuration;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var shouldAutoMigrate = app.Environment.IsDevelopment()
            || app.Configuration.GetValue<bool>("Database:AutoMigrate");

        if (db.Database.IsRelational() && shouldAutoMigrate)
        {
            await db.Database.MigrateAsync();
        }
        else if (!db.Database.IsRelational())
        {
            await db.Database.EnsureCreatedAsync();
        }

        var shouldSeedDemoUsers = app.Environment.IsDevelopment()
            || app.Configuration.GetValue<bool>("SeedDemoUsers");
        if (shouldSeedDemoUsers)
        {
            await DbInitializer.SeedAsync(db, app.Configuration);
        }
    }
}
