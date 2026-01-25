using Microsoft.Extensions.Diagnostics.HealthChecks;
using src.Services;

namespace src.HealthChecks;

/// <summary>
/// Health check for Cosmos DB connectivity.
/// Verifies that the database connection is operational.
/// </summary>
public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<CosmosDbHealthCheck> _logger;

    public CosmosDbHealthCheck(ICosmosDbService cosmosDbService, ILogger<CosmosDbHealthCheck> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt a lightweight operation to verify connectivity
            // GetUserPreferencesAsync with a non-existent user is a low-cost operation
            await _cosmosDbService.GetUserPreferencesAsync("health-check-probe");
            
            _logger.LogDebug("Cosmos DB health check passed");
            return HealthCheckResult.Healthy("Cosmos DB is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cosmos DB health check failed");
            return HealthCheckResult.Unhealthy(
                "Cosmos DB is not accessible",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "ErrorType", ex.GetType().Name },
                    { "ErrorMessage", ex.Message }
                });
        }
    }
}
