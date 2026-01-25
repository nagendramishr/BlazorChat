namespace src.Services.Cache;

/// <summary>
/// Thread state information for persistence.
/// </summary>
public class ThreadState
{
    public string ConversationId { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Service interface for persisting AI agent thread state.
/// Implementations can use Cosmos DB, Redis, or other storage backends.
/// </summary>
public interface IThreadStateService
{
    /// <summary>
    /// Gets the thread state for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Thread state if exists and not expired, null otherwise.</returns>
    Task<ThreadState?> GetThreadStateAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates thread state for a conversation.
    /// </summary>
    /// <param name="state">The thread state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetThreadStateAsync(ThreadState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes thread state for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveThreadStateAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a valid (non-expired) thread exists for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if valid thread exists.</returns>
    Task<bool> ThreadExistsAsync(string conversationId, CancellationToken cancellationToken = default);
}
