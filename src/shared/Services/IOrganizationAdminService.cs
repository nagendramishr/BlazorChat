using BlazorChat.Shared.Models;

namespace BlazorChat.Shared.Services;

/// <summary>
/// Admin operations for organization management (CRUD).
/// Used by OrganizationApplicationService and Admin portal.
/// </summary>
public interface IOrganizationAdminService
{
    Task<Organization> OnboardOrganizationAsync(Organization organization);
    Task<Organization> UpdateOrganizationAsync(string id, Organization organization);
    Task DisableOrganizationAsync(string id);
    Task EnableOrganizationAsync(string id);
    Task<IEnumerable<Organization>> ListOrganizationsAsync();
}
