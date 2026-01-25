using System.ComponentModel.DataAnnotations;

namespace src.Models;

/// <summary>
/// Validation model for chat message input.
/// </summary>
public class ChatMessageInput
{
    /// <summary>
    /// The message content from the user.
    /// </summary>
    [Required(ErrorMessage = "Message content is required")]
    [StringLength(32000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 32,000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The conversation ID this message belongs to.
    /// </summary>
    [StringLength(100, ErrorMessage = "Conversation ID cannot exceed 100 characters")]
    public string? ConversationId { get; set; }
}

/// <summary>
/// Validation model for conversation creation.
/// </summary>
public class ConversationInput
{
    /// <summary>
    /// Optional title for the conversation.
    /// </summary>
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    /// <summary>
    /// Optional organization ID for multi-tenant scenarios.
    /// </summary>
    [StringLength(100, ErrorMessage = "Organization ID cannot exceed 100 characters")]
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Validation model for organization input.
/// </summary>
public class OrganizationInput
{
    /// <summary>
    /// The display name of the organization.
    /// </summary>
    [Required(ErrorMessage = "Organization name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe slug for the organization (used in routes).
    /// </summary>
    [Required(ErrorMessage = "Slug is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Slug must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Primary brand color (hex format).
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Primary color must be a valid hex color")]
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex format).
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Secondary color must be a valid hex color")]
    public string? SecondaryColor { get; set; }
}
