namespace MothersonBoxManagement.Services;

public class LoginLockoutCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoginLockoutCleanupService> _logger;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public LoginLockoutCleanupService(IServiceProvider serviceProvider, ILogger<LoginLockoutCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var lockoutService = scope.ServiceProvider.GetRequiredService<ILoginLockoutService>();
                await lockoutService.CleanupExpiredEntriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup expired login attempts");
            }
        }
    }
}
