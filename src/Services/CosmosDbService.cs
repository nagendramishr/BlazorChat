using Azure.Identity;
using Microsoft.Azure.Cosmos;
using src.Models;
using System.Net;

namespace src.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _conversationsContainer;
    private readonly Container _messagesContainer;
    private readonly Container _preferencesContainer;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(IConfiguration configuration, ILogger<CosmosDbService> logger)
    {
        _logger = logger;

        var endpoint = configuration["CosmosDb:Endpoint"] 
            ?? throw new ArgumentNullException("CosmosDb:Endpoint", "Cosmos DB endpoint not configured");
        var databaseName = configuration["CosmosDb:DatabaseName"] 
            ?? throw new ArgumentNullException("CosmosDb:DatabaseName", "Cosmos DB database name not configured");

        // Use DefaultAzureCredential for authentication
        var clientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            ConnectionMode = ConnectionMode.Gateway, // Gateway mode is more reliable from WSL
            RequestTimeout = TimeSpan.FromSeconds(30),
            MaxRetryAttemptsOnRateLimitedRequests = 3,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
        };

        _cosmosClient = new CosmosClient(endpoint, new DefaultAzureCredential(), clientOptions);
        
        var database = _cosmosClient.GetDatabase(databaseName);
        _conversationsContainer = database.GetContainer(configuration["CosmosDb:ConversationsContainerName"] ?? "conversations");
        _messagesContainer = database.GetContainer(configuration["CosmosDb:MessagesContainerName"] ?? "messages");
        _preferencesContainer = database.GetContainer(configuration["CosmosDb:PreferencesContainerName"] ?? "preferences");

        _logger.LogInformation("CosmosDbService initialized with endpoint: {Endpoint}, database: {Database}", endpoint, databaseName);
    }

    #region Conversation Operations

    public async Task<Conversation?> GetConversationAsync(string conversationId, string userId)
    {
        try
        {
            var response = await _conversationsContainer.ReadItemAsync<Conversation>(
                conversationId, 
                new PartitionKey(conversationId));

            LogDiagnostics(response.Diagnostics, nameof(GetConversationAsync));
            
            // Security check: Ensure the requesting user owns the conversation
            if (response.Resource.UserId != userId)
            {
                _logger.LogWarning("Security violation: User {UserId} attempted to access conversation {ConversationId} owned by {OwnerId}", 
                    userId, conversationId, response.Resource.UserId);
                return null;
            }

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Conversation {ConversationId} not found for user {UserId}", conversationId, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<Conversation>> GetConversationsAsync(string userId, int maxItems = 50, string? continuationToken = null)
    {
        try
        {
            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId AND c.isDeleted = false ORDER BY c.updatedAt DESC")
                .WithParameter("@userId", userId);

            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = maxItems
            };

            var iterator = _conversationsContainer.GetItemQueryIterator<Conversation>(
                queryDefinition,
                continuationToken,
                queryRequestOptions);

            var conversations = new List<Conversation>();
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                LogDiagnostics(response.Diagnostics, nameof(GetConversationsAsync));
                conversations.AddRange(response);
            }

            return conversations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation)
    {
        try
        {
            conversation.CreatedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;

            var response = await _conversationsContainer.CreateItemAsync(
                conversation,
                new PartitionKey(conversation.Id));

            LogDiagnostics(response.Diagnostics, nameof(CreateConversationAsync));
            _logger.LogInformation("Created conversation {ConversationId} for user {UserId}", conversation.Id, conversation.UserId);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for user {UserId}", conversation.UserId);
            throw;
        }
    }

    public async Task<Conversation> UpdateConversationAsync(Conversation conversation)
    {
        try
        {
            conversation.UpdatedAt = DateTime.UtcNow;

            var response = await _conversationsContainer.ReplaceItemAsync(
                conversation,
                conversation.Id,
                new PartitionKey(conversation.Id));

            LogDiagnostics(response.Diagnostics, nameof(UpdateConversationAsync));
            _logger.LogInformation("Updated conversation {ConversationId}", conversation.Id);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation {ConversationId}", conversation.Id);
            throw;
        }
    }

    public async Task DeleteConversationAsync(string conversationId, string userId)
    {
        try
        {
            // Soft delete - mark as deleted rather than removing
            var conversation = await GetConversationAsync(conversationId, userId);
            if (conversation != null)
            {
                conversation.IsDeleted = true;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversationsContainer.ReplaceItemAsync(
                    conversation,
                    conversationId,
                    new PartitionKey(conversationId));

                _logger.LogInformation("Deleted conversation {ConversationId} for user {UserId}", conversationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            throw;
        }
    }

    #endregion

    #region Message Operations

    public async Task<Message?> GetMessageAsync(string messageId, string conversationId)
    {
        try
        {
            var response = await _messagesContainer.ReadItemAsync<Message>(
                messageId,
                new PartitionKey(messageId));

            LogDiagnostics(response.Diagnostics, nameof(GetMessageAsync));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Message {MessageId} not found in conversation {ConversationId}", messageId, conversationId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(string conversationId, int maxItems = 100, string? continuationToken = null)
    {
        try
        {
            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.conversationId = @conversationId AND c.isDeleted = false ORDER BY c.timestamp ASC")
                .WithParameter("@conversationId", conversationId);

            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = maxItems
            };

            var iterator = _messagesContainer.GetItemQueryIterator<Message>(
                queryDefinition,
                continuationToken,
                queryRequestOptions);

            var messages = new List<Message>();
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                LogDiagnostics(response.Diagnostics, nameof(GetMessagesAsync));
                messages.AddRange(response);
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<Message> SaveMessageAsync(Message message)
    {
        try
        {
            message.Timestamp = DateTime.UtcNow;

            var response = await _messagesContainer.CreateItemAsync(
                message,
                new PartitionKey(message.Id));

            LogDiagnostics(response.Diagnostics, nameof(SaveMessageAsync));
            _logger.LogInformation("Saved message {MessageId} in conversation {ConversationId}", message.Id, message.ConversationId);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message to conversation {ConversationId}", message.ConversationId);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> SaveMessagesAsync(IEnumerable<Message> messages)
    {
        var savedMessages = new List<Message>();
        
        foreach (var message in messages)
        {
            var savedMessage = await SaveMessageAsync(message);
            savedMessages.Add(savedMessage);
        }

        return savedMessages;
    }

    public async Task DeleteMessageAsync(string messageId, string conversationId)
    {
        try
        {
            // Soft delete
            var message = await GetMessageAsync(messageId, conversationId);
            if (message != null)
            {
                message.IsDeleted = true;
                await _messagesContainer.ReplaceItemAsync(
                    message,
                    messageId,
                    new PartitionKey(messageId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
            throw;
        }
    }

    #endregion

    #region User Preferences Operations

    public async Task<UserPreferences?> GetUserPreferencesAsync(string userId)
    {
        try
        {
            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var queryRequestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId),
                MaxItemCount = 1
            };

            var iterator = _preferencesContainer.GetItemQueryIterator<UserPreferences>(
                queryDefinition,
                requestOptions: queryRequestOptions);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                LogDiagnostics(response.Diagnostics, nameof(GetUserPreferencesAsync));
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserPreferences> SaveUserPreferencesAsync(UserPreferences preferences)
    {
        try
        {
            preferences.UpdatedAt = DateTime.UtcNow;

            var response = await _preferencesContainer.UpsertItemAsync(
                preferences,
                new PartitionKey(preferences.UserId));

            LogDiagnostics(response.Diagnostics, nameof(SaveUserPreferencesAsync));
            _logger.LogInformation("Saved preferences for user {UserId}", preferences.UserId);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving preferences for user {UserId}", preferences.UserId);
            throw;
        }
    }

    #endregion

    #region Utility Operations

    public async Task<int> GetConversationMessageCountAsync(string conversationId)
    {
        try
        {
            var queryDefinition = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.conversationId = @conversationId AND c.isDeleted = false")
                .WithParameter("@conversationId", conversationId);

            var queryRequestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(conversationId)
            };

            var iterator = _messagesContainer.GetItemQueryIterator<int>(
                queryDefinition,
                requestOptions: queryRequestOptions);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                LogDiagnostics(response.Diagnostics, nameof(GetConversationMessageCountAsync));
                return response.FirstOrDefault();
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message count for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> ConversationExistsAsync(string conversationId, string userId)
    {
        var conversation = await GetConversationAsync(conversationId, userId);
        return conversation != null && !conversation.IsDeleted;
    }

    #endregion

    #region Private Helper Methods

    private void LogDiagnostics(CosmosDiagnostics diagnostics, string operationName)
    {
        var diagnosticString = diagnostics.ToString();
        
        // Log if request took longer than threshold (e.g., 100ms)
        if (diagnostics.GetClientElapsedTime().TotalMilliseconds > 100)
        {
            _logger.LogWarning("Slow Cosmos DB operation {Operation}: {Diagnostics}", 
                operationName, diagnosticString);
        }
        else
        {
            _logger.LogDebug("Cosmos DB operation {Operation}: {Diagnostics}", 
                operationName, diagnosticString);
        }
    }

    #endregion
}
