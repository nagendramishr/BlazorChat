using BlazorChat.Shared.Models;
using src.Services.EventHub;
using System.Text.RegularExpressions;

namespace src.Services.Application;

/// <summary>
/// Application service for organization management.
/// Wraps IOrganizationAdminService with validation and event publishing.
/// </summary>
public class OrganizationApplicationService : IOrganizationApplicationService
{
    private readonly IOrganizationAdminService _adminService;
    private readonly IEventClient _eventClient;
    private readonly ILogger<OrganizationApplicationService> _logger;

    // Slug validation: lowercase letters, numbers, hyphens only, 3-50 chars
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$", RegexOptions.Compiled);

    public OrganizationApplicationService(
        IOrganizationAdminService adminService,
        IEventClient eventClient,
        ILogger<OrganizationApplicationService> logger)
    {
        _adminService = adminService;
        _eventClient = eventClient;
        _logger = logger;
    }

    public async Task<OrganizationResult> CreateOrganizationAsync(CreateOrganizationRequest request, string performedByUserId)
    {
        // Validation
        var errors = ValidateCreateRequest(request);
        if (errors.Any())
        {
            _logger.LogWarning("Organization creation validation failed: {Errors}", string.Join(", ", errors));
            return OrganizationResult.Fail(errors.ToArray());
        }

        try
        {
            var organization = new Organization
            {
                Name = request.Name.Trim(),
                Slug = request.Slug.ToLowerInvariant().Trim(),
                PublicThemeSettings = request.ThemeSettings ?? new ThemeSettings(),
                PrivateAIConfig = request.AIConfig ?? new AIConfig()
            };

            var created = await _adminService.OnboardOrganizationAsync(organization);

            // Publish event
            _eventClient.SendData(new OrganizationCreatedEvent
            {
                OrganizationId = created.Id,
                OrganizationSlug = created.Slug,
                OrganizationName = created.Name,
                PerformedByUserId = performedByUserId,
                HasCustomBranding = HasCustomBranding(created.PublicThemeSettings),
                HasCustomAIConfig = HasCustomAIConfig(created.PrivateAIConfig)
            });

            _logger.LogInformation("Organization created: {OrgId} ({Slug}) by user {UserId}", 
                created.Id, created.Slug, performedByUserId);

            return OrganizationResult.Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Organization creation failed: {Message}", ex.Message);
            return OrganizationResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating organization");
            return OrganizationResult.Fail("An unexpected error occurred while creating the organization.");
        }
    }

    public async Task<OrganizationResult> UpdateOrganizationAsync(string id, UpdateOrganizationRequest request, string performedByUserId)
    {
        // Get existing
        var existing = (await _adminService.ListOrganizationsAsync()).FirstOrDefault(o => o.Id == id);
        if (existing == null)
        {
            return OrganizationResult.NotFound(id);
        }

        // Validation
        var errors = ValidateUpdateRequest(request, existing);
        if (errors.Any())
        {
            _logger.LogWarning("Organization update validation failed: {Errors}", string.Join(", ", errors));
            return OrganizationResult.Fail(errors.ToArray());
        }

        try
        {
            // Track changes for event
            var changedFields = new List<string>();
            bool brandingChanged = false;
            bool aiConfigChanged = false;
            string? oldSlug = null;

            // Apply updates
            if (request.Name != null && request.Name != existing.Name)
            {
                changedFields.Add("Name");
                existing.Name = request.Name.Trim();
            }

            if (request.Slug != null && request.Slug.ToLowerInvariant() != existing.Slug)
            {
                changedFields.Add("Slug");
                oldSlug = existing.Slug;
                existing.Slug = request.Slug.ToLowerInvariant().Trim();
            }

            if (request.ThemeSettings != null)
            {
                brandingChanged = !ThemeSettingsEqual(existing.PublicThemeSettings, request.ThemeSettings);
                if (brandingChanged)
                {
                    changedFields.Add("ThemeSettings");
                    existing.PublicThemeSettings = request.ThemeSettings;
                }
            }

            if (request.AIConfig != null)
            {
                aiConfigChanged = !AIConfigEqual(existing.PrivateAIConfig, request.AIConfig);
                if (aiConfigChanged)
                {
                    changedFields.Add("AIConfig");
                    existing.PrivateAIConfig = request.AIConfig;
                }
            }

            if (!changedFields.Any())
            {
                return OrganizationResult.Ok(existing); // No changes
            }

            var updated = await _adminService.UpdateOrganizationAsync(id, existing);

            // Publish events
            _eventClient.SendData(new OrganizationUpdatedEvent
            {
                OrganizationId = updated.Id,
                OrganizationSlug = updated.Slug,
                OrganizationName = updated.Name,
                PerformedByUserId = performedByUserId,
                ChangedFields = changedFields.ToArray(),
                BrandingChanged = brandingChanged,
                AIConfigChanged = aiConfigChanged
            });

            if (oldSlug != null)
            {
                _eventClient.SendData(new OrganizationSlugChangedEvent
                {
                    OrganizationId = updated.Id,
                    OrganizationSlug = updated.Slug,
                    OrganizationName = updated.Name,
                    PerformedByUserId = performedByUserId,
                    OldSlug = oldSlug,
                    NewSlug = updated.Slug
                });
            }

            if (aiConfigChanged)
            {
                _eventClient.SendData(new OrganizationAIConfigChangedEvent
                {
                    OrganizationId = updated.Id,
                    OrganizationSlug = updated.Slug,
                    OrganizationName = updated.Name,
                    PerformedByUserId = performedByUserId,
                    EndpointChanged = existing.PrivateAIConfig.Endpoint != request.AIConfig?.Endpoint,
                    ModelChanged = existing.PrivateAIConfig.ModelDeployment != request.AIConfig?.ModelDeployment,
                    AgentChanged = existing.PrivateAIConfig.AgentName != request.AIConfig?.AgentName,
                    SystemPromptChanged = existing.PrivateAIConfig.SystemPrompt != request.AIConfig?.SystemPrompt
                });
            }

            _logger.LogInformation("Organization updated: {OrgId} fields [{Fields}] by user {UserId}",
                updated.Id, string.Join(", ", changedFields), performedByUserId);

            return OrganizationResult.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Organization update failed: {Message}", ex.Message);
            return OrganizationResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating organization {OrgId}", id);
            return OrganizationResult.Fail("An unexpected error occurred while updating the organization.");
        }
    }

    public async Task<OrganizationResult> DisableOrganizationAsync(string id, string? reason, string performedByUserId)
    {
        try
        {
            var orgs = await _adminService.ListOrganizationsAsync();
            var org = orgs.FirstOrDefault(o => o.Id == id);
            if (org == null)
            {
                return OrganizationResult.NotFound(id);
            }

            if (org.IsDisabled)
            {
                return OrganizationResult.Fail("Organization is already disabled.");
            }

            await _adminService.DisableOrganizationAsync(id);

            // Publish event
            _eventClient.SendData(new OrganizationDisabledEvent
            {
                OrganizationId = org.Id,
                OrganizationSlug = org.Slug,
                OrganizationName = org.Name,
                PerformedByUserId = performedByUserId,
                Reason = reason
            });

            _logger.LogInformation("Organization disabled: {OrgId} ({Slug}) by user {UserId}. Reason: {Reason}",
                org.Id, org.Slug, performedByUserId, reason ?? "Not specified");

            org.IsDisabled = true;
            return OrganizationResult.Ok(org);
        }
        catch (KeyNotFoundException)
        {
            return OrganizationResult.NotFound(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error disabling organization {OrgId}", id);
            return OrganizationResult.Fail("An unexpected error occurred while disabling the organization.");
        }
    }

    public async Task<OrganizationResult> EnableOrganizationAsync(string id, string performedByUserId)
    {
        try
        {
            var orgs = await _adminService.ListOrganizationsAsync();
            var org = orgs.FirstOrDefault(o => o.Id == id);
            if (org == null)
            {
                return OrganizationResult.NotFound(id);
            }

            if (!org.IsDisabled)
            {
                return OrganizationResult.Fail("Organization is already enabled.");
            }

            await _adminService.EnableOrganizationAsync(id);

            // Publish event
            _eventClient.SendData(new OrganizationEnabledEvent
            {
                OrganizationId = org.Id,
                OrganizationSlug = org.Slug,
                OrganizationName = org.Name,
                PerformedByUserId = performedByUserId
            });

            _logger.LogInformation("Organization enabled: {OrgId} ({Slug}) by user {UserId}",
                org.Id, org.Slug, performedByUserId);

            org.IsDisabled = false;
            return OrganizationResult.Ok(org);
        }
        catch (KeyNotFoundException)
        {
            return OrganizationResult.NotFound(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error enabling organization {OrgId}", id);
            return OrganizationResult.Fail("An unexpected error occurred while enabling the organization.");
        }
    }

    public async Task<Organization?> GetOrganizationAsync(string id)
    {
        var orgs = await _adminService.ListOrganizationsAsync();
        return orgs.FirstOrDefault(o => o.Id == id);
    }

    public async Task<IEnumerable<Organization>> ListOrganizationsAsync()
    {
        return await _adminService.ListOrganizationsAsync();
    }

    #region Validation

    private List<string> ValidateCreateRequest(CreateOrganizationRequest request)
    {
        var errors = new List<string>();

        // Name validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Organization name is required.");
        }
        else if (request.Name.Length < 2 || request.Name.Length > 100)
        {
            errors.Add("Organization name must be between 2 and 100 characters.");
        }

        // Slug validation
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            errors.Add("Organization slug is required.");
        }
        else if (!SlugRegex.IsMatch(request.Slug.ToLowerInvariant()))
        {
            errors.Add("Slug must be 3-50 characters, lowercase letters, numbers, and hyphens only. Cannot start or end with hyphen.");
        }

        // Reserved slugs
        var reservedSlugs = new[] { "admin", "api", "system", "default", "public", "private", "internal", "org", "auth", "login", "logout" };
        if (reservedSlugs.Contains(request.Slug?.ToLowerInvariant()))
        {
            errors.Add($"Slug '{request.Slug}' is reserved and cannot be used.");
        }

        // AI Config validation (optional but if provided, must be valid)
        if (request.AIConfig != null)
        {
            errors.AddRange(ValidateAIConfig(request.AIConfig));
        }

        return errors;
    }

    private List<string> ValidateUpdateRequest(UpdateOrganizationRequest request, Organization existing)
    {
        var errors = new List<string>();

        // Name validation (if provided)
        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Organization name cannot be empty.");
            }
            else if (request.Name.Length < 2 || request.Name.Length > 100)
            {
                errors.Add("Organization name must be between 2 and 100 characters.");
            }
        }

        // Slug validation (if provided)
        if (request.Slug != null)
        {
            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                errors.Add("Organization slug cannot be empty.");
            }
            else if (!SlugRegex.IsMatch(request.Slug.ToLowerInvariant()))
            {
                errors.Add("Slug must be 3-50 characters, lowercase letters, numbers, and hyphens only.");
            }

            var reservedSlugs = new[] { "admin", "api", "system", "default", "public", "private", "internal", "org", "auth", "login", "logout" };
            if (reservedSlugs.Contains(request.Slug?.ToLowerInvariant()))
            {
                errors.Add($"Slug '{request.Slug}' is reserved and cannot be used.");
            }
        }

        // AI Config validation (if provided)
        if (request.AIConfig != null)
        {
            errors.AddRange(ValidateAIConfig(request.AIConfig));
        }

        return errors;
    }

    private List<string> ValidateAIConfig(AIConfig config)
    {
        var errors = new List<string>();

        // Endpoint validation (if provided, must be valid URL)
        if (!string.IsNullOrWhiteSpace(config.Endpoint))
        {
            if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                errors.Add("AI endpoint must be a valid HTTP/HTTPS URL.");
            }
        }

        // System prompt length limit
        if (config.SystemPrompt?.Length > 10000)
        {
            errors.Add("System prompt cannot exceed 10,000 characters.");
        }

        // Agent instructions length limit
        if (config.AgentInstructions?.Length > 10000)
        {
            errors.Add("Agent instructions cannot exceed 10,000 characters.");
        }

        return errors;
    }

    #endregion

    #region Helpers

    private static bool HasCustomBranding(ThemeSettings settings)
    {
        var defaults = new ThemeSettings();
        return settings.PrimaryColor != defaults.PrimaryColor ||
               settings.SecondaryColor != defaults.SecondaryColor ||
               !string.IsNullOrEmpty(settings.LogoUrl) ||
               !string.IsNullOrEmpty(settings.CustomCss) ||
               !string.IsNullOrEmpty(settings.HeaderHtml) ||
               !string.IsNullOrEmpty(settings.FooterHtml);
    }

    private static bool HasCustomAIConfig(AIConfig config)
    {
        return !string.IsNullOrEmpty(config.Endpoint) ||
               !string.IsNullOrEmpty(config.ModelDeployment) ||
               !string.IsNullOrEmpty(config.AgentName) ||
               !string.IsNullOrEmpty(config.SystemPrompt);
    }

    private static bool ThemeSettingsEqual(ThemeSettings a, ThemeSettings b)
    {
        return a.PrimaryColor == b.PrimaryColor &&
               a.SecondaryColor == b.SecondaryColor &&
               a.LogoUrl == b.LogoUrl &&
               a.CustomCss == b.CustomCss &&
               a.HeaderHtml == b.HeaderHtml &&
               a.FooterHtml == b.FooterHtml;
    }

    private static bool AIConfigEqual(AIConfig a, AIConfig b)
    {
        return a.Endpoint == b.Endpoint &&
               a.ModelDeployment == b.ModelDeployment &&
               a.AgentName == b.AgentName &&
               a.AgentInstructions == b.AgentInstructions &&
               a.SystemPrompt == b.SystemPrompt;
    }

    #endregion
}
