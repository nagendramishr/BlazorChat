namespace src.Services.Cache;

/// <summary>
/// In-memory thread state service for development/testing.
/// NOT suitable for production multi-instance deployments.
/// </summary>
public class InMemoryThreadStateService : IThreadStateService
{
    private readonly Dictionary<string, ThreadState> _cache = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryThreadStateService> _logger;

    public InMemoryThreadStateService(ILogger<InMemoryThreadStateService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using InMemoryThreadStateService - not suitable for production multi-instance deployments");
    }

    public Task<ThreadState?> GetThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(conversationId, out var state))
            {
                if (state.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogDebug("Retrieved thread state from memory for conversation {ConversationId}", conversationId);
                    return Task.FromResult<ThreadState?>(state);
                }
                else
                {
                    _cache.Remove(conversationId);
                    _logger.LogDebug("Thread state expired for conversation {ConversationId}", conversationId);
                }
            }
        }
        
        return Task.FromResult<ThreadState?>(null);
    }

    public Task SetThreadStateAsync(ThreadState state, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache[state.ConversationId] = state;
            _logger.LogDebug("Saved thread state to memory for conversation {ConversationId}", state.ConversationId);
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache.Remove(conversationId);
            _logger.LogDebug("Removed thread state from memory for conversation {ConversationId}", conversationId);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> ThreadExistsAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(conversationId, out var state))
            {
                return Task.FromResult(state.IsActive && state.ExpiresAt > DateTime.UtcNow);
            }
        }
        
        return Task.FromResult(false);
    }
}
