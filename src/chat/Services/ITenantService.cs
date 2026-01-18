using src.Models;

namespace src.Services;

public interface ITenantService
{
    Organization? CurrentTenant { get; }
    bool IsLoaded { get; }
    Task<bool> LoadTenantAsync(string slug);
}
