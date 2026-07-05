using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserListItemDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _context.Users
            .OrderBy(u => u.Matricule)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Matricule = u.Matricule,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Users.FindAsync(new object[] { id }, ct);
    }

    public async Task<User?> GetUserByMatriculeAsync(string matricule, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Matricule == matricule, ct);
    }

    public async Task<User> CreateUserAsync(string matricule, string fullName, string role, string password, CancellationToken ct = default)
    {
        if (await _context.Users.AnyAsync(u => u.Matricule == matricule, ct))
            throw new InvalidOperationException($"A user with matricule '{matricule}' already exists.");

        var user = new User
        {
            Matricule = matricule,
            FullName = fullName,
            Role = role,
            IsActive = true,
            PasswordHash = _passwordHasher.HashPassword(null!, password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> UpdateUserAsync(int id, string fullName, string role, bool isActive, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"User {id} was not found.");

        user.FullName = fullName;
        user.Role = role;
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"User {id} was not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteUserAsync(int id, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException($"User {id} was not found.");

        user.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }
}
