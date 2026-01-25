using BlazorChat.Shared.Models;

namespace src.Services;

public interface IOrganizationAdminService
{
    Task<Organization> OnboardOrganizationAsync(Organization organization);
    Task<Organization> UpdateOrganizationAsync(string id, Organization organization);
    Task DisableOrganizationAsync(string id);
    Task EnableOrganizationAsync(string id);
    Task<IEnumerable<Organization>> ListOrganizationsAsync();
}
