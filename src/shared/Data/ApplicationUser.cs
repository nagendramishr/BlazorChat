using Microsoft.AspNetCore.Identity;

namespace BlazorChat.Shared.Data;

/// <summary>
/// Application user with organization association.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The organization this user belongs to.
    /// </summary>
    public string? OrganizationId { get; set; }
}
