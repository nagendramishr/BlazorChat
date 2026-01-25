using System.Text.Json.Serialization;

namespace BlazorChat.Shared.Models;

public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public MessageRole Role { get; set; } = MessageRole.User;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional metadata for tracking tokens, model info, etc.
    /// </summary>
    [JsonPropertyName("metadata")]
    public MessageMetadata? Metadata { get; set; }

    /// <summary>
    /// Optional: Flag for soft delete
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class MessageMetadata
{
    [JsonPropertyName("tokensUsed")]
    public int? TokensUsed { get; set; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; }

    [JsonPropertyName("responseTime")]
    public double? ResponseTimeMs { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("additionalData")]
    public Dictionary<string, object>? AdditionalData { get; set; }
}
