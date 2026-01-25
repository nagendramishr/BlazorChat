using Microsoft.Extensions.Diagnostics.HealthChecks;
using src.Services;

namespace src.HealthChecks;

/// <summary>
/// Health check for AI Foundry service connectivity.
/// Verifies that the AI agent can be initialized and is responsive.
/// </summary>
public class AIFoundryHealthCheck : IHealthCheck
{
    private readonly IAIFoundryService _aiFoundryService;
    private readonly ILogger<AIFoundryHealthCheck> _logger;

    public AIFoundryHealthCheck(IAIFoundryService aiFoundryService, ILogger<AIFoundryHealthCheck> logger)
    {
        _aiFoundryService = aiFoundryService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize the AI agent if not already initialized
            // This is an idempotent operation
            await _aiFoundryService.InitializeAsync(cancellationToken);
            
            _logger.LogDebug("AI Foundry health check passed");
            return HealthCheckResult.Healthy("AI Foundry agent is initialized and accessible");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
        {
            // AI Foundry not configured - this is degraded, not unhealthy
            // The app can function without AI (just won't have AI responses)
            _logger.LogWarning(ex, "AI Foundry is not configured");
            return HealthCheckResult.Degraded(
                "AI Foundry is not configured",
                data: new Dictionary<string, object>
                {
                    { "Reason", "Configuration missing" },
                    { "Impact", "AI chat responses will not be available" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Foundry health check failed");
            return HealthCheckResult.Unhealthy(
                "AI Foundry is not accessible",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "ErrorType", ex.GetType().Name },
                    { "ErrorMessage", ex.Message }
                });
        }
    }
}
