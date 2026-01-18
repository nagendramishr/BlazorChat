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
    /// Optional: Total message count in this conversation
    /// </summary>
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Optional: Flag for soft delete
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
