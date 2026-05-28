using System.Data;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kanban.Api.Health;

public sealed class DatabaseHealthCheck(IDbConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.ExecuteScalarAsync("SELECT 1");
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not available.", ex);
        }
    }
}
