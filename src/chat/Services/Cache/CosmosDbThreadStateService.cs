using src.Models;

namespace src.Services.Cache;

/// <summary>
/// Thread state service implementation using Cosmos DB.
/// Stores thread info directly in the Conversation document.
/// Best for: Single-instance deployments, simpler infrastructure.
/// </summary>
public class CosmosDbThreadStateService : IThreadStateService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<CosmosDbThreadStateService> _logger;

    public CosmosDbThreadStateService(
        ICosmosDbService cosmosDbService,
        ILogger<CosmosDbThreadStateService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task<ThreadState?> GetThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // We need to get the conversation to access thread info
            // Note: This requires knowing the userId, which we don't have here.
            // For Cosmos DB implementation, we'll use a different approach - 
            // the ChatApplicationService will pass the thread info from the Conversation it already has.
            
            _logger.LogDebug("GetThreadStateAsync called for {ConversationId} - Cosmos DB implementation uses conversation document", 
                conversationId);
            
            // Return null - the caller should use the Conversation.AgentThreadId directly
            // This is a limitation of the Cosmos DB approach without userId
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thread state for conversation {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task SetThreadStateAsync(ThreadState state, CancellationToken cancellationToken = default)
    {
        // For Cosmos DB, thread state is stored in the Conversation document.
        // The ChatApplicationService handles this by updating Conversation.AgentThreadId, etc.
        // This method is a no-op for Cosmos DB as we update via the conversation.
        
        _logger.LogDebug("SetThreadStateAsync called for {ConversationId} - state saved via Conversation update", 
            state.ConversationId);
        
        await Task.CompletedTask;
    }

    public async Task RemoveThreadStateAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        // For Cosmos DB, clearing thread state means setting AgentThreadId to null on the Conversation.
        // This is handled by the ChatApplicationService.
        
        _logger.LogDebug("RemoveThreadStateAsync called for {ConversationId} - cleared via Conversation update", 
            conversationId);
        
        await Task.CompletedTask;
    }

    public async Task<bool> ThreadExistsAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        // For Cosmos DB, check if Conversation has a non-expired thread.
        // This is handled by the caller checking Conversation.AgentThreadId.
        
        _logger.LogDebug("ThreadExistsAsync called for {ConversationId} - check via Conversation.AgentThreadId", 
            conversationId);
        
        return await Task.FromResult(false);
    }
}
