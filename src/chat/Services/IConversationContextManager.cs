using BlazorChat.Shared.Models;

namespace src.Services;

/// <summary>
/// Interface for managing conversation context and token usage
/// </summary>
public interface IConversationContextManager
{
    /// <summary>
    /// Estimate token count for messages
    /// </summary>
    int EstimateTokenCount(IEnumerable<Message> messages);

    /// <summary>
    /// Trim messages to fit within token limit
    /// </summary>
    IEnumerable<Message> TrimMessages(IEnumerable<Message> messages, int maxTokens);

    /// <summary>
    /// Check if messages exceed token limit
    /// </summary>
    bool ExceedsTokenLimit(IEnumerable<Message> messages, int maxTokens);

    /// <summary>
    /// Get recommended context window for conversation
    /// </summary>
    int GetRecommendedContextWindow();

    /// <summary>
    /// Create a summary of trimmed messages (placeholder for future implementation)
    /// </summary>
    Task<string> SummarizeMessagesAsync(IEnumerable<Message> messages);
}
