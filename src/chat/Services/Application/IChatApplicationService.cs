using src.Models;

namespace src.Services.Application;

/// <summary>
/// Application service that orchestrates chat operations.
/// Acts as the single entry point for all chat-related business logic,
/// encapsulating authorization, validation, persistence, and AI interactions.
/// </summary>
public interface IChatApplicationService
{
    /// <summary>
    /// Creates a new conversation for the user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="organizationId">Optional organization ID.</param>
    /// <param name="title">Optional title (defaults to "New Conversation").</param>
    /// <returns>The created conversation.</returns>
    Task<Conversation> CreateConversationAsync(string userId, string? organizationId = null, string? title = null);

    /// <summary>
    /// Gets a conversation by ID with authorization check.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The requesting user's ID.</param>
    /// <returns>The conversation if found and authorized, null otherwise.</returns>
    Task<Conversation?> GetConversationAsync(string conversationId, string userId);

    /// <summary>
    /// Gets all conversations for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="maxItems">Maximum number of conversations to return.</param>
    /// <returns>List of user's conversations.</returns>
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, int maxItems = 50);

    /// <summary>
    /// Gets messages for a conversation with authorization check.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The requesting user's ID.</param>
    /// <param name="maxItems">Maximum number of messages to return.</param>
    /// <returns>List of messages if authorized, empty list otherwise.</returns>
    Task<IEnumerable<Message>> GetMessagesAsync(string conversationId, string userId, int maxItems = 100);

    /// <summary>
    /// Sends a message and streams the AI response.
    /// Handles: saving user message, calling AI, streaming response, saving AI response.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="message">The message content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of AI response chunks.</returns>
    IAsyncEnumerable<ChatResponseChunk> SendMessageStreamingAsync(
        string conversationId, 
        string userId, 
        string message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation with authorization check.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The requesting user's ID.</param>
    /// <returns>True if deleted, false if not found or unauthorized.</returns>
    Task<bool> DeleteConversationAsync(string conversationId, string userId);

    /// <summary>
    /// Resumes a conversation, rehydrating the AI thread if needed.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if conversation was successfully resumed.</returns>
    Task<bool> ResumeConversationAsync(string conversationId, string userId);

    /// <summary>
    /// Updates a conversation's title.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userId">The requesting user's ID.</param>
    /// <param name="newTitle">The new title.</param>
    /// <returns>Updated conversation if successful, null otherwise.</returns>
    Task<Conversation?> UpdateConversationTitleAsync(string conversationId, string userId, string newTitle);
}

/// <summary>
/// Represents a chunk of the chat response during streaming.
/// </summary>
public class ChatResponseChunk
{
    /// <summary>
    /// The text content of this chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The full message ID (available once streaming starts).
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Indicates if this is the final chunk.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Error message if something went wrong.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates if the message was saved successfully to persistence.
    /// Only set on the final chunk.
    /// </summary>
    public bool? WasSaved { get; set; }
}
