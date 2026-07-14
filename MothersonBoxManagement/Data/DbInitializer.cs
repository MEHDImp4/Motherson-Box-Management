using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var passwordHasher = new PasswordHasher<User>();
        var definitions = new[]
        {
            (Matricule: "OP001", FullName: "Test Operator", Role: "Operator"),
            (Matricule: "SP001", FullName: "Test Supervisor", Role: "Supervisor"),
            (Matricule: "AD001", FullName: "Test Administrator", Role: "Administrator")
        };
        var passwords = definitions.ToDictionary(
            definition => definition.Matricule,
            definition => configuration[$"DemoUsers:{definition.Matricule}:Password"] ?? string.Empty);
        if (passwords.Values.Any(password => string.IsNullOrWhiteSpace(password) || password.Length < 16) ||
            passwords.Values.Distinct(StringComparer.Ordinal).Count() != definitions.Length)
        {
            throw new InvalidOperationException(
                "DemoUsers requires three distinct external passwords of at least 16 characters for OP001, SP001 and AD001.");
        }

        bool changesMade = false;
        foreach (var definition in definitions)
        {
            var existingUser = await context.Users.FirstOrDefaultAsync(
                u => u.Matricule == definition.Matricule,
                cancellationToken);
            if (existingUser is null)
            {
                var user = new User
                {
                    Matricule = definition.Matricule,
                    FullName = definition.FullName,
                    Role = definition.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = passwordHasher.HashPassword(user, passwords[definition.Matricule]);
                user.SecurityStamp = Guid.NewGuid().ToString("N");
                context.Users.Add(user);
                changesMade = true;
            }
            else if (string.IsNullOrWhiteSpace(existingUser.SecurityStamp))
            {
                existingUser.SecurityStamp = Guid.NewGuid().ToString("N");
                changesMade = true;
            }
        }

        if (changesMade)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
