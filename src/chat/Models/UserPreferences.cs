using System.Text.Json.Serialization;

namespace src.Models;

public class UserPreferences
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Partition key for Cosmos DB - using UserId
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey => UserId;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "light";

    [JsonPropertyName("notificationSettings")]
    public NotificationSettings NotificationSettings { get; set; } = new();

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "UTC";

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: AI-specific preferences
    /// </summary>
    [JsonPropertyName("aiPreferences")]
    public AIPreferences? AIPreferences { get; set; }
}

public class NotificationSettings
{
    [JsonPropertyName("emailNotifications")]
    public bool EmailNotifications { get; set; } = true;

    [JsonPropertyName("pushNotifications")]
    public bool PushNotifications { get; set; } = true;

    [JsonPropertyName("soundEnabled")]
    public bool SoundEnabled { get; set; } = true;
}

public class AIPreferences
{
    [JsonPropertyName("defaultModel")]
    public string? DefaultModel { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 2000;

    [JsonPropertyName("streamResponses")]
    public bool StreamResponses { get; set; } = true;
}
