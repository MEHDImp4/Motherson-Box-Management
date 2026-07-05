using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var passwordHasher = new PasswordHasher<User>();
        const string defaultPassword = "Motherson2026!";

        var users = new[]
        {
            new User { Matricule = "OP001", FullName = "Test Operator", Role = "Operator", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Matricule = "SP001", FullName = "Test Supervisor", Role = "Supervisor", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Matricule = "AD001", FullName = "Test Administrator", Role = "Administrator", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        bool changesMade = false;
        foreach (var user in users)
        {
            if (!await context.Users.AnyAsync(u => u.Matricule == user.Matricule))
            {
                user.PasswordHash = passwordHasher.HashPassword(user, defaultPassword);
                context.Users.Add(user);
                changesMade = true;
            }
        }

        if (changesMade)
        {
            await context.SaveChangesAsync();
        }
    }
}
