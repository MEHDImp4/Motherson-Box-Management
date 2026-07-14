using System.Security.Claims;
using System.Threading.RateLimiting;

namespace MothersonBoxManagement.Configuration;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("login", httpContext =>
            {
                var matricule = (httpContext.Request.Form["Matricule"].FirstOrDefault() ?? "unknown")
                    .Trim().ToUpperInvariant();
                var client = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
                return RateLimitPartition.GetTokenBucketLimiter($"{client}:{matricule}", _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    TokensPerPeriod = 5,
                    AutoReplenishment = true
                });
            });

            options.AddPolicy("scan", httpContext =>
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            options.AddPolicy("global", httpContext =>
            {
                var partition = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1)
                });
            });

            options.AddPolicy("print-agent", httpContext =>
            {
                var credential = httpContext.Request.Headers.Authorization.ToString();
                var partition = string.IsNullOrWhiteSpace(credential)
                    ? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous-agent"
                    : MothersonBoxManagement.Printing.PrintAgentSecurity.Hash(credential);
                return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
