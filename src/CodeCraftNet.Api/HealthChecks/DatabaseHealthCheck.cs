using Microsoft.Extensions.Diagnostics.HealthChecks;
using CodeCraftNet.Infrastructure.Persistence;

namespace CodeCraftNet.Api.HealthChecks;

public sealed class DatabaseHealthCheck(CodeCraftNetDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database check threw.", exception);
        }
    }
}
