using ClickHouse.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DemoApi.Health;

public sealed class ClickHouseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new ClickHouseClient(
                configuration["ClickHouse:ConnectionString"]!);
            await client.ExecuteScalarAsync("SELECT 1");
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClickHouse is unreachable", ex);
        }
    }
}
