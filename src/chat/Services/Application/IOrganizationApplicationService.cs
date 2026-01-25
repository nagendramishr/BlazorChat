using BlazorChat.Shared.Models;

namespace src.Services.Application;

/// <summary>
/// Application service for organization management operations.
/// Wraps IOrganizationAdminService with validation and event publishing.
/// Use IOrganizationService for read-only current org context in UI components.
/// </summary>
public interface IOrganizationApplicationService
{
    /// <summary>
    /// Creates a new organization with validation.
    /// Publishes OrganizationCreatedEvent on success.
    /// </summary>
    Task<OrganizationResult> CreateOrganizationAsync(CreateOrganizationRequest request, string performedByUserId);

    /// <summary>
    /// Updates an existing organization with validation.
    /// Publishes OrganizationUpdatedEvent on success.
    /// </summary>
    Task<OrganizationResult> UpdateOrganizationAsync(string id, UpdateOrganizationRequest request, string performedByUserId);

    /// <summary>
    /// Disables an organization (soft disable, not delete).
    /// Publishes OrganizationDisabledEvent on success.
    /// </summary>
    Task<OrganizationResult> DisableOrganizationAsync(string id, string? reason, string performedByUserId);

    /// <summary>
    /// Re-enables a disabled organization.
    /// Publishes OrganizationEnabledEvent on success.
    /// </summary>
    Task<OrganizationResult> EnableOrganizationAsync(string id, string performedByUserId);

    /// <summary>
    /// Gets an organization by ID.
    /// </summary>
    Task<Organization?> GetOrganizationAsync(string id);

    /// <summary>
    /// Lists all organizations (for admin use).
    /// </summary>
    Task<IEnumerable<Organization>> ListOrganizationsAsync();
}

/// <summary>
/// Request model for creating an organization.
/// </summary>
public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ThemeSettings? ThemeSettings { get; set; }
    public AIConfig? AIConfig { get; set; }
}

/// <summary>
/// Request model for updating an organization.
/// </summary>
public class UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public ThemeSettings? ThemeSettings { get; set; }
    public AIConfig? AIConfig { get; set; }
}

/// <summary>
/// Result of an organization operation.
/// </summary>
public class OrganizationResult
{
    public bool Success { get; set; }
    public Organization? Organization { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static OrganizationResult Ok(Organization org) => new() { Success = true, Organization = org };
    public static OrganizationResult Fail(params string[] errors) => new() { Success = false, Errors = errors.ToList() };
    public static OrganizationResult NotFound(string id) => Fail($"Organization '{id}' not found.");
}
