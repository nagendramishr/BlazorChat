using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace src.Services.Cache;

/// <summary>
/// Thread state service implementation using Redis distributed cache.
/// Best for: Multi-instance deployments, horizontal scaling, fast lookups.
/// </summary>
public class RedisThreadStateService : IThreadStateService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisThreadStateService> _logger;
    private readonly TimeSpan _defaultExpiry;

    private const string KeyPrefix = "thread:";

    public RedisThreadStateService(
        IDistributedCache cache,
        ILogger<RedisThreadStateService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        
        // Default expiry from config or 24 hours
        var expiryHours = configuration.GetValue<int>("ThreadState:ExpiryHours", 24);
        _defaultExpiry = TimeSpan.FromHours(expiryHours);
    }

    private static string GetKey(string conversationId) => $"{KeyPrefix}{conversationId}";

    public async Task<ThreadState?> GetThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetKey(conversationId);
            var data = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogDebug("Thread state not found in cache for conversation {ConversationId}", conversationId);
                return null;
            }

            var state = JsonSerializer.Deserialize<ThreadState>(data);
            
            if (state == null)
            {
                _logger.LogWarning("Failed to deserialize thread state for conversation {ConversationId}", conversationId);
                return null;
            }

            // Check if expired
            if (state.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogInformation("Thread state expired for conversation {ConversationId}", conversationId);
                await RemoveThreadStateAsync(conversationId, cancellationToken);
                return null;
            }

            _logger.LogDebug("Retrieved thread state from cache for conversation {ConversationId}", conversationId);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thread state from Redis for conversation {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task SetThreadStateAsync(ThreadState state, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetKey(state.ConversationId);
            var data = JsonSerializer.Serialize(state);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = state.ExpiresAt
            };

            await _cache.SetStringAsync(key, data, options, cancellationToken);
            
            _logger.LogDebug("Saved thread state to cache for conversation {ConversationId}, expires {ExpiresAt}", 
                state.ConversationId, state.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving thread state to Redis for conversation {ConversationId}", 
                state.ConversationId);
            throw;
        }
    }

    public async Task RemoveThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetKey(conversationId);
            await _cache.RemoveAsync(key, cancellationToken);
            
            _logger.LogDebug("Removed thread state from cache for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing thread state from Redis for conversation {ConversationId}", 
                conversationId);
        }
    }

    public async Task<bool> ThreadExistsAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var state = await GetThreadStateAsync(conversationId, cancellationToken);
        return state != null && state.IsActive && state.ExpiresAt > DateTime.UtcNow;
    }
}
