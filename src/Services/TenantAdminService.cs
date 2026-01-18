using src.Models;
using Microsoft.Extensions.Logging;

namespace src.Services;

public class TenantAdminService : ITenantAdminService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<TenantAdminService> _logger;

    public TenantAdminService(ICosmosDbService cosmosDbService, ILogger<TenantAdminService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<Organization> OnboardOrganizationAsync(Organization organization)
    {
        // 1. Validate Slug Uniqueness
        var existing = await _cosmosDbService.GetOrganizationBySlugAsync(organization.Slug);
        if (existing != null)
        {
            throw new InvalidOperationException($"Organization with slug '{organization.Slug}' already exists.");
        }

        // 2. Create
        organization.Id = Guid.NewGuid().ToString(); // Ensure ID is generated
        organization.IsDisabled = false;
        
        return await _cosmosDbService.CreateOrganizationAsync(organization);
    }

    public async Task<Organization> UpdateOrganizationAsync(string id, Organization organization)
    {
        var existing = await _cosmosDbService.GetOrganizationAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Organization {id} not found.");
        }

        // Preserve ID
        organization.Id = id;
        
        // Check slug change collision if slug is changing
        if (existing.Slug != organization.Slug)
        {
            var conflict = await _cosmosDbService.GetOrganizationBySlugAsync(organization.Slug);
            if (conflict != null)
            {
                 throw new InvalidOperationException($"Slug '{organization.Slug}' is already taken.");
            }
        }

        return await _cosmosDbService.UpdateOrganizationAsync(organization);
    }

    public async Task DisableOrganizationAsync(string id)
    {
        var org = await _cosmosDbService.GetOrganizationAsync(id);
        if (org == null) throw new KeyNotFoundException($"Organization {id} not found.");

        org.IsDisabled = true;
        await _cosmosDbService.UpdateOrganizationAsync(org);
    }

    public async Task EnableOrganizationAsync(string id)
    {
        var org = await _cosmosDbService.GetOrganizationAsync(id);
        if (org == null) throw new KeyNotFoundException($"Organization {id} not found.");

        org.IsDisabled = false;
        await _cosmosDbService.UpdateOrganizationAsync(org);
    }

    public async Task<IEnumerable<Organization>> ListOrganizationsAsync()
    {
        return await _cosmosDbService.ListOrganizationsAsync();
    }
}
