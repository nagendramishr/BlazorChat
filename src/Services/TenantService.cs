using src.Models;

namespace src.Services;

public class TenantService : ITenantService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<TenantService> _logger;

    public Organization? CurrentTenant { get; private set; }
    public bool IsLoaded { get; private set; }

    public TenantService(ICosmosDbService cosmosDbService, ILogger<TenantService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<bool> LoadTenantAsync(string slug)
    {
        try
        {
            // If already loaded for this slug, skip (optimization for signalr/re-renders)
            if (IsLoaded && CurrentTenant?.Slug == slug)
            {
                return true;
            }

            _logger.LogInformation("Resolving tenant for slug: {Slug}", slug);
            
            var organization = await _cosmosDbService.GetOrganizationBySlugAsync(slug);
            
            if (organization != null)
            {
                CurrentTenant = organization;
                IsLoaded = true;
                return true;
            }
            
            _logger.LogWarning("Tenant not found for slug: {Slug}", slug);
            CurrentTenant = null;
            IsLoaded = true; // Loaded, but found nothing
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tenant {Slug}", slug);
            return false;
        }
    }
}
