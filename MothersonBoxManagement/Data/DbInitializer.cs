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
            new User { Matricule = "OP001", Role = "Operator", IsActive = true },
            new User { Matricule = "SP001", Role = "Supervisor", IsActive = true },
            new User { Matricule = "AD001", Role = "Administrator", IsActive = true }
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
