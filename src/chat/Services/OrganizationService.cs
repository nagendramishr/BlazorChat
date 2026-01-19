using src.Models;

namespace src.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<OrganizationService> _logger;

    public Organization? CurrentOrganization { get; private set; }
    public bool IsLoaded { get; private set; }

    public OrganizationService(ICosmosDbService cosmosDbService, ILogger<OrganizationService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<bool> LoadOrganizationAsync(string slug)
    {
        try
        {
            // If already loaded for this slug, skip (optimization for signalr/re-renders)
            if (IsLoaded && CurrentOrganization?.Slug == slug)
            {
                return true;
            }

            _logger.LogInformation("Resolving organization for slug: {Slug}", slug);
            
            var organization = await _cosmosDbService.GetOrganizationBySlugAsync(slug);
            
            if (organization != null)
            {
                CurrentOrganization = organization;
                IsLoaded = true;
                return true;
            }
            
            _logger.LogWarning("Organization not found for slug: {Slug}", slug);
            CurrentOrganization = null;
            IsLoaded = true; // Loaded, but found nothing
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organization {Slug}", slug);
            return false;
        }
    }
}
