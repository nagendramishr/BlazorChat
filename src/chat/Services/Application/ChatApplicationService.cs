using System.Runtime.CompilerServices;
using System.Threading.Channels;
using src.Models;
using src.Services.Cache;

namespace src.Services.Application;

/// <summary>
/// Application service that orchestrates chat operations.
/// Encapsulates authorization, validation, Cosmos DB calls, AI calls, and telemetry.
/// </summary>
public class ChatApplicationService : IChatApplicationService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly IConversationContextManager _contextManager;
    private readonly ITelemetryService _telemetryService;
    private readonly IMessageSanitizationService _sanitizationService;
    private readonly IThreadStateService _threadStateService;
    private readonly ILogger<ChatApplicationService> _logger;

    // Configuration
    private const int DefaultMaxTokens = 6000;
    private const int TrimmedTokenLimit = 4000;
    private const int MaxMessageLength = 10000;
    private const int TitleMaxLength = 50;

    public ChatApplicationService(
        ICosmosDbService cosmosDbService,
        IAIFoundryService aiFoundryService,
        IConversationContextManager contextManager,
        ITelemetryService telemetryService,
        IMessageSanitizationService sanitizationService,
        IThreadStateService threadStateService,
        ILogger<ChatApplicationService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _aiFoundryService = aiFoundryService;
        _contextManager = contextManager;
        _telemetryService = telemetryService;
        _sanitizationService = sanitizationService;
        _threadStateService = threadStateService;
        _logger = logger;
    }

    public async Task<Conversation> CreateConversationAsync(string userId, string? organizationId = null, string? title = null)
    {
        _logger.LogInformation("Creating new conversation for user {UserId}", userId);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            OrganizationId = organizationId,
            Title = title ?? "New Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            MessageCount = 0
        };

        var savedConversation = await _cosmosDbService.CreateConversationAsync(conversation);

        _telemetryService.TrackEvent("ConversationCreated", new Dictionary<string, string>
        {
            { "UserId", userId },
            { "ConversationId", savedConversation.Id },
            { "OrganizationId", organizationId ?? "none" }
        });

        _logger.LogInformation("Created conversation {ConversationId} for user {UserId}", savedConversation.Id, userId);
        return savedConversation;
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId, string userId)
    {
        var conversation = await _cosmosDbService.GetConversationAsync(conversationId, userId);

        if (conversation == null)
        {
            _logger.LogWarning("Conversation {ConversationId} not found for user {UserId}", conversationId, userId);
            return null;
        }

        // Authorization check - user must own the conversation
        if (conversation.UserId != userId)
        {
            _logger.LogWarning("Authorization failed: User {UserId} attempted to access conversation {ConversationId} owned by {OwnerId}",
                userId, conversationId, conversation.UserId);
            return null;
        }

        return conversation;
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, int maxItems = 50)
    {
        _logger.LogInformation("Loading conversations for user {UserId}", userId);

        try
        {
            var conversations = await _cosmosDbService.GetConversationsAsync(userId, maxItems);
            var conversationList = conversations.ToList();

            _logger.LogInformation("Loaded {Count} conversations for user {UserId}", conversationList.Count, userId);
            return conversationList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load conversations for user {UserId}", userId);
            _telemetryService.TrackException(ex, userId, null);
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(string conversationId, string userId, int maxItems = 100)
    {
        // Verify user owns the conversation
        var conversation = await GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            _logger.LogWarning("Cannot load messages: conversation {ConversationId} not found or unauthorized for user {UserId}",
                conversationId, userId);
            return Enumerable.Empty<Message>();
        }

        try
        {
            var messages = await _cosmosDbService.GetMessagesAsync(conversationId, maxItems);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load messages for conversation {ConversationId}", conversationId);
            _telemetryService.TrackException(ex, userId, conversationId);
            throw;
        }
    }

    public async IAsyncEnumerable<ChatResponseChunk> SendMessageStreamingAsync(
        string conversationId,
        string userId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(message))
        {
            yield return new ChatResponseChunk { Error = "Message cannot be empty", IsComplete = true };
            yield break;
        }

        if (message.Length > MaxMessageLength)
        {
            yield return new ChatResponseChunk { Error = $"Message exceeds maximum length of {MaxMessageLength} characters", IsComplete = true };
            yield break;
        }

        // Verify user owns the conversation
        var conversation = await GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            yield return new ChatResponseChunk { Error = "Conversation not found or access denied", IsComplete = true };
            yield break;
        }

        // Sanitize the message
        var sanitizedMessage = _sanitizationService.SanitizeMessage(message);

        // Check for potential prompt injection
        if (_sanitizationService.ContainsPromptInjection(sanitizedMessage))
        {
            _logger.LogWarning("Potential prompt injection detected from user {UserId} in conversation {ConversationId}",
                userId, conversationId);
            _telemetryService.TrackEvent("PromptInjectionAttempt", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "ConversationId", conversationId }
            });
            // Continue but log the attempt - may want to block in production
        }

        // Create and save user message
        var userMessage = new Message
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            UserId = userId,
            Content = sanitizedMessage,
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow
        };

        // Save user message - if this fails, we need to stop
        var saveResult = await TrySaveUserMessageAsync(userMessage, conversationId);
        if (!saveResult.Success)
        {
            yield return new ChatResponseChunk { Error = saveResult.Error, IsComplete = true };
            yield break;
        }

        // Update conversation metadata
        await UpdateConversationAfterMessage(conversation, sanitizedMessage, isFirstMessage: conversation.MessageCount == 0);

        // Check context limits
        var existingMessages = (await _cosmosDbService.GetMessagesAsync(conversationId, 100)).ToList();
        if (_contextManager.ExceedsTokenLimit(existingMessages, DefaultMaxTokens))
        {
            var trimmedMessages = _contextManager.TrimMessages(existingMessages, TrimmedTokenLimit).ToList();
            var estimatedTokens = _contextManager.EstimateTokenCount(trimmedMessages);
            _telemetryService.TrackConversationMetrics(conversationId, existingMessages.Count, estimatedTokens);
        }

        // Use a channel to collect chunks from the AI service
        var channel = Channel.CreateUnbounded<ChatResponseChunk>();
        var assistantMessageId = Guid.NewGuid().ToString();
        
        // Start the AI streaming in a background task
        _ = StreamAIResponseAsync(channel.Writer, conversationId, userId, sanitizedMessage, assistantMessageId, conversation, cancellationToken);

        // Yield chunks from the channel
        await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task<(bool Success, string? Error)> TrySaveUserMessageAsync(Message userMessage, string conversationId)
    {
        try
        {
            await _cosmosDbService.SaveMessageAsync(userMessage);
            _telemetryService.TrackMessageSent(userMessage.UserId, conversationId, userMessage.Content.Length);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user message for conversation {ConversationId}", conversationId);
            return (false, "Failed to save message");
        }
    }

    private async Task StreamAIResponseAsync(
        ChannelWriter<ChatResponseChunk> writer,
        string conversationId,
        string userId,
        string sanitizedMessage,
        string assistantMessageId,
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        var responseStartTime = DateTime.UtcNow;
        var fullResponse = new System.Text.StringBuilder();

        try
        {
            // Check thread state service first (for Redis/distributed cache)
            var cachedState = await _threadStateService.GetThreadStateAsync(conversationId, cancellationToken);
            var existingThreadId = cachedState?.ThreadId ?? conversation.AgentThreadId;
            
            await foreach (var textChunk in _aiFoundryService.SendMessageStreamingAsync(
                conversationId, sanitizedMessage, existingThreadId, cancellationToken))
            {
                fullResponse.Append(textChunk);
                await writer.WriteAsync(new ChatResponseChunk
                {
                    Content = textChunk,
                    MessageId = assistantMessageId,
                    IsComplete = false
                }, cancellationToken);
            }

            // Capture thread info for persistence
            var threadInfo = _aiFoundryService.GetThreadInfo(conversationId);
            if (threadInfo != null && threadInfo.IsNewThread)
            {
                // New thread was created - save to both cache and conversation
                var threadState = new Cache.ThreadState
                {
                    ConversationId = conversationId,
                    ThreadId = threadInfo.ThreadId,
                    CreatedAt = threadInfo.CreatedAt,
                    ExpiresAt = threadInfo.ExpiresAt,
                    IsActive = true
                };
                
                // Save to thread state service (Redis or Cosmos DB based on config)
                await _threadStateService.SetThreadStateAsync(threadState, cancellationToken);
                
                // Also update conversation for Cosmos DB fallback
                conversation.AgentThreadId = threadInfo.ThreadId;
                conversation.AgentThreadCreatedAt = threadInfo.CreatedAt;
                conversation.AgentThreadExpiry = threadInfo.ExpiresAt;
                
                _logger.LogInformation("Saved new thread {ThreadId} to conversation {ConversationId}",
                    threadInfo.ThreadId, conversationId);
            }

            // Save assistant message
            var assistantMessage = new Message
            {
                Id = assistantMessageId,
                ConversationId = conversationId,
                UserId = userId,
                Content = fullResponse.ToString(),
                Role = MessageRole.Assistant,
                Timestamp = DateTime.UtcNow
            };

            bool wasSaved = false;
            try
            {
                await _cosmosDbService.SaveMessageAsync(assistantMessage);
                wasSaved = true;

                // Track telemetry
                var responseTimeMs = (DateTime.UtcNow - responseStartTime).TotalMilliseconds;
                _telemetryService.TrackMessageReceived(conversationId, assistantMessage.Content.Length, responseTimeMs);
                _telemetryService.TrackAIResponseTime(conversationId, responseTimeMs, isStreaming: true);

                // Update conversation
                conversation.MessageCount++;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _cosmosDbService.UpdateConversationAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save assistant message for conversation {ConversationId}", conversationId);
                _telemetryService.TrackException(ex, userId, conversationId);
            }

            // Send final success chunk
            await writer.WriteAsync(new ChatResponseChunk
            {
                Content = string.Empty,
                MessageId = assistantMessageId,
                IsComplete = true,
                WasSaved = wasSaved
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AI response cancelled for conversation {ConversationId}", conversationId);
            await writer.WriteAsync(new ChatResponseChunk
            {
                Error = "Response cancelled",
                MessageId = assistantMessageId,
                IsComplete = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service error for conversation {ConversationId}", conversationId);
            _telemetryService.TrackException(ex, userId, conversationId, new Dictionary<string, string>
            {
                { "Operation", "AIResponse" },
                { "MessageLength", sanitizedMessage.Length.ToString() }
            });

            // Save error message
            var errorMessage = new Message
            {
                Id = assistantMessageId,
                ConversationId = conversationId,
                UserId = userId,
                Content = "Sorry, I encountered an error processing your request. Please try again.",
                Role = MessageRole.Assistant,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                await _cosmosDbService.SaveMessageAsync(errorMessage);
            }
            catch
            {
                // Ignore save failures for error messages
            }

            await writer.WriteAsync(new ChatResponseChunk
            {
                Content = errorMessage.Content,
                MessageId = assistantMessageId,
                Error = ex.Message,
                IsComplete = true
            });
        }
        finally
        {
            writer.Complete();
        }
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, string userId)
    {
        // Verify user owns the conversation
        var conversation = await GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            _logger.LogWarning("Cannot delete: conversation {ConversationId} not found or unauthorized for user {UserId}",
                conversationId, userId);
            return false;
        }

        try
        {
            // Delete from Cosmos DB
            await _cosmosDbService.DeleteConversationAsync(conversationId, userId);

            // Clear AI thread
            await _aiFoundryService.ClearConversationAsync(conversationId);

            _telemetryService.TrackEvent("ConversationDeleted", new Dictionary<string, string>
            {
                { "UserId", userId },
                { "ConversationId", conversationId }
            });

            _logger.LogInformation("Deleted conversation {ConversationId} for user {UserId}", conversationId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete conversation {ConversationId}", conversationId);
            _telemetryService.TrackException(ex, userId, conversationId);
            throw;
        }
    }

    public async Task<bool> ResumeConversationAsync(string conversationId, string userId)
    {
        // Verify user owns the conversation
        var conversation = await GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            return false;
        }

        _logger.LogInformation("Resuming conversation {ConversationId} for user {UserId}", conversationId, userId);

        // TODO: Implement thread rehydration when A3 (Redis) is complete
        // For now, just log the resume - the AI service will create a new thread if needed

        _telemetryService.TrackEvent("ConversationResumed", new Dictionary<string, string>
        {
            { "UserId", userId },
            { "ConversationId", conversationId },
            { "MessageCount", conversation.MessageCount.ToString() }
        });

        return true;
    }

    public async Task<Conversation?> UpdateConversationTitleAsync(string conversationId, string userId, string newTitle)
    {
        var conversation = await GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            return null;
        }

        // Sanitize and truncate title
        var sanitizedTitle = _sanitizationService.SanitizeMessage(newTitle);
        if (sanitizedTitle.Length > TitleMaxLength)
        {
            sanitizedTitle = sanitizedTitle.Substring(0, TitleMaxLength - 3) + "...";
        }

        conversation.Title = sanitizedTitle;
        conversation.UpdatedAt = DateTime.UtcNow;

        return await _cosmosDbService.UpdateConversationAsync(conversation);
    }

    private async Task UpdateConversationAfterMessage(Conversation conversation, string message, bool isFirstMessage)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        conversation.MessageCount++;

        // Auto-generate title from first message
        if (isFirstMessage && conversation.Title == "New Conversation")
        {
            conversation.Title = message.Length > TitleMaxLength
                ? message.Substring(0, TitleMaxLength - 3) + "..."
                : message;
        }

        await _cosmosDbService.UpdateConversationAsync(conversation);
    }
}
