namespace src.Services;

/// <summary>
/// Thread information returned when a thread is created or resumed.
/// </summary>
public class ThreadInfo
{
    public string ThreadId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsNewThread { get; set; }
}

/// <summary>
/// Service interface for interacting with Microsoft Foundry AI agents.
/// </summary>
public interface IAIFoundryService
{
    /// <summary>
    /// Sends a message to the AI agent and gets a streaming response.
    /// </summary>
    /// <param name="conversationId">The conversation ID for context persistence.</param>
    /// <param name="message">The user's message.</param>
    /// <param name="existingThreadId">Optional existing thread ID to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response text chunks.</returns>
    IAsyncEnumerable<string> SendMessageStreamingAsync(
        string conversationId, 
        string message,
        string? existingThreadId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the AI agent and gets a complete response.
    /// Useful for testing or non-UI scenarios.
    /// </summary>
    /// <param name="conversationId">The conversation ID for context persistence.</param>
    /// <param name="message">The user's message.</param>
    /// <param name="existingThreadId">Optional existing thread ID to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response from the AI agent.</returns>
    Task<string> SendMessageAsync(
        string conversationId, 
        string message,
        string? existingThreadId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current thread info for a conversation, or null if no thread exists.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <returns>Thread info if exists, null otherwise.</returns>
    ThreadInfo? GetThreadInfo(string conversationId);

    /// <summary>
    /// Clears the conversation thread for a given conversation ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID to clear.</param>
    Task ClearConversationAsync(string conversationId);

    /// <summary>
    /// Ensures the AI agent is initialized and ready to use.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
