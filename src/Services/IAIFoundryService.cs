namespace src.Services;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response text chunks.</returns>
    IAsyncEnumerable<string> SendMessageStreamingAsync(
        string conversationId, 
        string message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the AI agent and gets a complete response.
    /// Useful for testing or non-UI scenarios.
    /// </summary>
    /// <param name="conversationId">The conversation ID for context persistence.</param>
    /// <param name="message">The user's message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response from the AI agent.</returns>
    Task<string> SendMessageAsync(
        string conversationId, 
        string message, 
        CancellationToken cancellationToken = default);

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
