using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using System.Collections.Concurrent;
using System.Data;

namespace MothersonBoxManagement.Services;

public interface ILoginLockoutService
{
    Task<bool> IsLockedOutAsync(string matricule, CancellationToken cancellationToken = default);
    Task RecordFailedAttemptAsync(string matricule, CancellationToken cancellationToken = default);
    Task ResetAttemptsAsync(string matricule, CancellationToken cancellationToken = default);
    Task<int> GetRemainingAttemptsAsync(string matricule, CancellationToken cancellationToken = default);
    Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default);
}

public class LoginLockoutService : ILoginLockoutService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> MatriculeGates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;

    public LoginLockoutService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _maxAttempts = configuration.GetValue("Security:LoginLockout:MaxAttempts", 5);
        _lockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("Security:LoginLockout:LockoutMinutes", 15));
    }

    public async Task<bool> IsLockedOutAsync(string matricule, CancellationToken cancellationToken = default)
    {
        matricule = matricule.Trim().ToUpperInvariant();
        var gate = MatriculeGates.GetOrAdd(matricule, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var attempt = await db.LoginAttempts.FirstOrDefaultAsync(la => la.Matricule == matricule, cancellationToken);

            if (attempt is null)
                return false;

            if (attempt.LockoutEnd.HasValue && attempt.LockoutEnd.Value > DateTime.UtcNow)
                return true;

            if (attempt.LockoutEnd.HasValue && attempt.LockoutEnd.Value <= DateTime.UtcNow)
            {
                db.LoginAttempts.Remove(attempt);
                await db.SaveChangesAsync(cancellationToken);
            }

            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RecordFailedAttemptAsync(string matricule, CancellationToken cancellationToken = default)
    {
        var normalized = matricule.Trim().ToUpperInvariant();
        var gate = MatriculeGates.GetOrAdd(normalized, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            var attempt = await db.LoginAttempts
                .FirstOrDefaultAsync(la => la.Matricule == normalized, cancellationToken);

            if (attempt is null)
            {
                attempt = new LoginAttempt
                {
                    Matricule = normalized,
                    FailedAttempts = 1,
                    LastAttemptAt = DateTime.UtcNow
                };
                db.LoginAttempts.Add(attempt);
            }
            else
            {
                attempt.FailedAttempts++;
                attempt.LastAttemptAt = DateTime.UtcNow;
            }

            if (attempt.FailedAttempts >= _maxAttempts)
                attempt.LockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ResetAttemptsAsync(string matricule, CancellationToken cancellationToken = default)
    {
        matricule = matricule.Trim().ToUpperInvariant();
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempt = await db.LoginAttempts.FirstOrDefaultAsync(la => la.Matricule == matricule, cancellationToken);
        if (attempt is not null)
        {
            db.LoginAttempts.Remove(attempt);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetRemainingAttemptsAsync(string matricule, CancellationToken cancellationToken = default)
    {
        matricule = matricule.Trim().ToUpperInvariant();
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempt = await db.LoginAttempts.FirstOrDefaultAsync(la => la.Matricule == matricule, cancellationToken);

        if (attempt is null)
            return _maxAttempts;

        if (attempt.LockoutEnd.HasValue && attempt.LockoutEnd.Value > DateTime.UtcNow)
            return 0;

        return Math.Max(0, _maxAttempts - attempt.FailedAttempts);
    }

    public async Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expiryThreshold = DateTime.UtcNow.AddHours(-1);
        var expired = await db.LoginAttempts
            .Where(la => la.LockoutEnd.HasValue && la.LockoutEnd.Value < expiryThreshold)
            .ToListAsync(cancellationToken);

        if (expired.Count > 0)
        {
            db.LoginAttempts.RemoveRange(expired);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
