namespace MothersonBoxManagement.Services;

public class LoginLockoutCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public LoginLockoutCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
            catch
            {
            }
        }
    }
}
