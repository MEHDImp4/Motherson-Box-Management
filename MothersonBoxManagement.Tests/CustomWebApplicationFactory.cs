using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            var passwordHasher = new PasswordHasher<User>();
            const string password = "Motherson2026!";

            var users = new[]
            {
                new User { Matricule = "OP001", Role = "Operator", IsActive = true },
                new User { Matricule = "SP001", Role = "Supervisor", IsActive = true },
                new User { Matricule = "AD001", Role = "Administrator", IsActive = true }
            };

            foreach (var user in users)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, password);
            }

            db.Users.AddRange(users);
            db.SaveChanges();
        });
    }
}
