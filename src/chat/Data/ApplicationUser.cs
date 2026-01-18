using Microsoft.AspNetCore.Identity;

namespace src.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string? OrganizationId { get; set; }
}

