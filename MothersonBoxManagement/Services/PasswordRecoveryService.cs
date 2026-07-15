using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;

namespace MothersonBoxManagement.Services;

public interface IPasswordRecoveryService
{
    Task RequestAsync(string matricule, string? ipAddress, CancellationToken ct = default);
    Task<IReadOnlyList<PasswordResetRequest>> GetPendingAsync(CancellationToken ct = default);
    Task<int> CountPendingAsync(CancellationToken ct = default);
    Task ApproveAsync(int requestId, int administratorId, CancellationToken ct = default);
    Task<(User User, PasswordResetRequest Request)?> BeginRecoveryAsync(string matricule, CancellationToken ct = default);
    Task<User> CompleteRecoveryAsync(int requestId, int userId, string newPassword, CancellationToken ct = default);
    Task<User> CompleteFirstLoginPasswordChangeAsync(int userId, string newPassword, CancellationToken ct = default);
}

public sealed class PasswordRecoveryService : IPasswordRecoveryService
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string InProgress = "InProgress";
    public const string Consumed = "Consumed";
    public const string Expired = "Expired";
    private static readonly TimeSpan ApprovalLifetime = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordRecoveryService(ApplicationDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task RequestAsync(string matricule, string? ipAddress, CancellationToken ct = default)
    {
        var normalized = (matricule ?? string.Empty).Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(candidate =>
            candidate.Matricule == normalized && candidate.IsActive && candidate.Id != SystemPrincipal.UserId, ct);
        if (user is null)
            return;

        var now = DateTime.UtcNow;
        var existing = await _db.PasswordResetRequests
            .Where(request => request.UserId == user.Id &&
                (request.Status == Pending || request.Status == Approved || request.Status == InProgress))
            .OrderByDescending(request => request.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (existing.Status == Pending || existing.ExpiresAt > now)
                return;
            existing.Status = Expired;
        }

        _db.PasswordResetRequests.Add(new PasswordResetRequest
        {
            UserId = user.Id,
            Status = Pending,
            RequestedAt = now,
            RequestedFromIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress[..Math.Min(ipAddress.Length, 64)]
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PasswordResetRequest>> GetPendingAsync(CancellationToken ct = default) =>
        await _db.PasswordResetRequests.AsNoTracking()
            .Include(request => request.User)
            .Where(request => request.Status == Pending)
            .OrderBy(request => request.RequestedAt)
            .ToListAsync(ct);

    public Task<int> CountPendingAsync(CancellationToken ct = default) =>
        _db.PasswordResetRequests.CountAsync(request => request.Status == Pending, ct);

    public async Task ApproveAsync(int requestId, int administratorId, CancellationToken ct = default)
    {
        var administrator = await _db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == administratorId, ct)
            ?? throw new InvalidOperationException("Administrator account was not found.");
        if (administrator.Role is not AppRoles.Administrator and not AppRoles.AdminFr)
            throw new UnauthorizedAccessException("Only an administrator can approve password recovery.");

        var request = await _db.PasswordResetRequests
            .Include(candidate => candidate.User)
            .FirstOrDefaultAsync(candidate => candidate.Id == requestId, ct)
            ?? throw new KeyNotFoundException("Password reset request was not found.");
        if (request.Status != Pending)
            throw new InvalidOperationException("This password reset request is no longer pending.");

        var now = DateTime.UtcNow;
        request.Status = Approved;
        request.ApprovedAt = now;
        request.ApprovedByUserId = administratorId;
        request.ExpiresAt = now.Add(ApprovalLifetime);
        var unusablePassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        request.User.PasswordHash = _passwordHasher.HashPassword(request.User, unusablePassword);
        request.User.SecurityStamp = Guid.NewGuid().ToString("N");
        request.User.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(User User, PasswordResetRequest Request)?> BeginRecoveryAsync(string matricule, CancellationToken ct = default)
    {
        var normalized = (matricule ?? string.Empty).Trim().ToUpperInvariant();
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var now = DateTime.UtcNow;
        var request = await _db.PasswordResetRequests
            .Include(candidate => candidate.User)
            .Where(candidate => candidate.User.Matricule == normalized && candidate.User.IsActive &&
                candidate.Status == Approved && candidate.ExpiresAt > now && candidate.StartedAt == null)
            .OrderByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync(ct);
        if (request is null)
            return null;

        request.Status = InProgress;
        request.StartedAt = now;
        await _db.SaveChangesAsync(ct);
        if (transaction is not null)
            await transaction.CommitAsync(ct);
        return (request.User, request);
    }

    public async Task<User> CompleteRecoveryAsync(int requestId, int userId, string newPassword, CancellationToken ct = default)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var request = await _db.PasswordResetRequests
            .Include(candidate => candidate.User)
            .FirstOrDefaultAsync(candidate => candidate.Id == requestId && candidate.UserId == userId, ct)
            ?? throw new InvalidOperationException("Password recovery session is invalid.");
        if (request.Status != InProgress || request.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Password recovery session has expired.");

        request.User.PasswordHash = _passwordHasher.HashPassword(request.User, newPassword);
        request.User.SecurityStamp = Guid.NewGuid().ToString("N");
        request.User.UpdatedAt = DateTime.UtcNow;
        request.Status = Consumed;
        request.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        if (transaction is not null)
            await transaction.CommitAsync(ct);
        return request.User;
    }

    public async Task<User> CompleteFirstLoginPasswordChangeAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new InvalidOperationException("User was not found.");
        if (!user.MustChangePassword)
            throw new InvalidOperationException("This account does not require a password change.");

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return user;
    }
}
