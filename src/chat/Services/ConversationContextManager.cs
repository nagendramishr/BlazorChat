using src.Models;

namespace src.Services;

/// <summary>
/// Service for managing conversation context and token limits
/// Uses approximate token counting (1 token ≈ 4 characters for English text)
/// </summary>
public class ConversationContextManager : IConversationContextManager
{
    private readonly ILogger<ConversationContextManager> _logger;
    private const int DEFAULT_MAX_TOKENS = 4096; // Conservative default
    private const int CHARS_PER_TOKEN = 4; // Rough approximation
    private const int SYSTEM_MESSAGE_TOKEN_BUFFER = 200; // Reserve for system prompts

    public ConversationContextManager(ILogger<ConversationContextManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Estimate token count for messages (approximate: 1 token ≈ 4 characters)
    /// </summary>
    public int EstimateTokenCount(IEnumerable<Message> messages)
    {
        var totalChars = messages.Sum(m => 
            (m.Content?.Length ?? 0) + 
            (m.Role == MessageRole.User ? 10 : 15) // Add overhead for role labels
        );
        
        var estimatedTokens = (int)Math.Ceiling(totalChars / (double)CHARS_PER_TOKEN);
        
        _logger.LogDebug(
            "Estimated {TokenCount} tokens for {MessageCount} messages ({CharCount} chars)",
            estimatedTokens, 
            messages.Count(), 
            totalChars);
        
        return estimatedTokens;
    }

    /// <summary>
    /// Trim messages to fit within token limit (keeps most recent messages)
    /// </summary>
    public IEnumerable<Message> TrimMessages(IEnumerable<Message> messages, int maxTokens)
    {
        var messageList = messages.OrderByDescending(m => m.Timestamp).ToList();
        var availableTokens = maxTokens - SYSTEM_MESSAGE_TOKEN_BUFFER;
        
        var trimmedMessages = new List<Message>();
        var currentTokens = 0;

        foreach (var message in messageList)
        {
            var messageTokens = EstimateTokenCount(new[] { message });
            
            if (currentTokens + messageTokens <= availableTokens)
            {
                trimmedMessages.Add(message);
                currentTokens += messageTokens;
            }
            else
            {
                _logger.LogInformation(
                    "Trimming conversation: kept {KeptCount} of {TotalCount} messages ({TokenCount}/{MaxTokens} tokens)",
                    trimmedMessages.Count,
                    messageList.Count,
                    currentTokens,
                    maxTokens);
                break;
            }
        }

        // Return in chronological order
        return trimmedMessages.OrderBy(m => m.Timestamp);
    }

    /// <summary>
    /// Check if messages exceed token limit
    /// </summary>
    public bool ExceedsTokenLimit(IEnumerable<Message> messages, int maxTokens)
    {
        var tokenCount = EstimateTokenCount(messages);
        var exceedsLimit = tokenCount > maxTokens;
        
        if (exceedsLimit)
        {
            _logger.LogWarning(
                "Conversation exceeds token limit: {TokenCount}/{MaxTokens} tokens",
                tokenCount,
                maxTokens);
        }
        
        return exceedsLimit;
    }

    /// <summary>
    /// Get recommended context window (conservative default)
    /// </summary>
    public int GetRecommendedContextWindow()
    {
        // Return conservative default for most models
        // Can be made configurable later based on model type
        return DEFAULT_MAX_TOKENS;
    }

    /// <summary>
    /// Create a summary of trimmed messages (placeholder for future AI-powered summarization)
    /// </summary>
    public async Task<string> SummarizeMessagesAsync(IEnumerable<Message> messages)
    {
        // TODO: Implement AI-powered summarization
        // For now, return a simple placeholder
        await Task.CompletedTask;
        
        var messageCount = messages.Count();
        var firstMessage = messages.FirstOrDefault();
        var lastMessage = messages.LastOrDefault();
        
        if (messageCount == 0)
        {
            return string.Empty;
        }
        
        var summary = $"[Earlier conversation with {messageCount} messages";
        
        if (firstMessage != null && lastMessage != null)
        {
            var timeSpan = lastMessage.Timestamp - firstMessage.Timestamp;
            if (timeSpan.TotalDays > 1)
            {
                summary += $" spanning {timeSpan.Days} days";
            }
            else if (timeSpan.TotalHours > 1)
            {
                summary += $" over {(int)timeSpan.TotalHours} hours";
            }
        }
        
        summary += "]";
        
        _logger.LogInformation("Generated conversation summary: {Summary}", summary);
        
        return summary;
    }
}
