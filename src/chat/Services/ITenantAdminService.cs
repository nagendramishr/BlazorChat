using src.Models;

namespace src.Services;

public interface ITenantAdminService
{
    Task<Organization> OnboardOrganizationAsync(Organization organization);
    Task<Organization> UpdateOrganizationAsync(string id, Organization organization);
    Task DisableOrganizationAsync(string id);
    Task EnableOrganizationAsync(string id);
    Task<IEnumerable<Organization>> ListOrganizationsAsync();
}
