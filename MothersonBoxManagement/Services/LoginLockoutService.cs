using System.Collections.Concurrent;

namespace MothersonBoxManagement.Services;

public interface ILoginLockoutService
{
    bool IsLockedOut(string matricule);
    void RecordFailedAttempt(string matricule);
    void ResetAttempts(string matricule);
    int GetRemainingAttempts(string matricule);
}

public class LoginLockoutService : ILoginLockoutService
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;
    private static readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();

    public LoginLockoutService(IConfiguration configuration)
    {
        _maxAttempts = configuration.GetValue("Security:LoginLockout:MaxAttempts", 5);
        _lockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("Security:LoginLockout:LockoutMinutes", 15));
    }

    public bool IsLockedOut(string matricule)
    {
        if (!_attempts.TryGetValue(matricule, out var info))
            return false;

        if (info.LockoutEnd.HasValue && info.LockoutEnd.Value > DateTime.UtcNow)
            return true;

        if (info.LockoutEnd.HasValue && info.LockoutEnd.Value <= DateTime.UtcNow)
        {
            info.FailedAttempts = 0;
            info.LockoutEnd = null;
        }

        return false;
    }

    public void RecordFailedAttempt(string matricule)
    {
        var info = _attempts.GetOrAdd(matricule, _ => new LoginAttemptInfo());

        info.FailedAttempts++;
        info.LastAttempt = DateTime.UtcNow;

        if (info.FailedAttempts >= _maxAttempts)
        {
            info.LockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
        }
    }

    public void ResetAttempts(string matricule)
    {
        _attempts.TryRemove(matricule, out _);
    }

    public int GetRemainingAttempts(string matricule)
    {
        if (!_attempts.TryGetValue(matricule, out var info))
            return _maxAttempts;

        if (info.LockoutEnd.HasValue && info.LockoutEnd.Value > DateTime.UtcNow)
            return 0;

        return Math.Max(0, _maxAttempts - info.FailedAttempts);
    }

    private class LoginAttemptInfo
    {
        public int FailedAttempts { get; set; }
        public DateTime LastAttempt { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
