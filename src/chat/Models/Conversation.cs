using System.Text.Json.Serialization;

namespace src.Models;

public class Conversation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = "New Conversation";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: Organization this conversation belongs to
    /// </summary>
    [JsonPropertyName("organizationId")]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Optional: Total message count in this conversation
    /// </summary>
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Optional: Flag for soft delete
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// AI Agent Thread ID for conversation continuity across sessions.
    /// Stored to allow resuming conversations with full context.
    /// </summary>
    [JsonPropertyName("agentThreadId")]
    public string? AgentThreadId { get; set; }

    /// <summary>
    /// When the agent thread was created.
    /// Used to track thread age for expiry management.
    /// </summary>
    [JsonPropertyName("agentThreadCreatedAt")]
    public DateTime? AgentThreadCreatedAt { get; set; }

    /// <summary>
    /// When the agent thread expires.
    /// After expiry, a new thread should be created.
    /// </summary>
    [JsonPropertyName("agentThreadExpiry")]
    public DateTime? AgentThreadExpiry { get; set; }
}
