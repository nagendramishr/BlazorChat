using BlazorChat.Shared.Models;

namespace src.Services;

public interface IOrganizationService
{
    Organization? CurrentOrganization { get; }
    bool IsLoaded { get; }
    Task<bool> LoadOrganizationAsync(string slug);
}
