using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCareMS.API.Health;

public sealed class DatabaseReadinessHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseReadinessHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HealthCareDbContext>();

        try
        {
            if (dbContext.Database.IsRelational())
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Database connection could not be established.");
                }
            }
            else
            {
                _ = await dbContext.Roles
                    .AsNoTracking()
                    .Take(1)
                    .AnyAsync(cancellationToken);
            }

            return HealthCheckResult.Healthy("Database connectivity is healthy.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database readiness health check failed.");
            return HealthCheckResult.Unhealthy("Database readiness check failed.", exception);
        }
    }
}
