using src.Models;

namespace src.Services;

public interface ICosmosDbService
{
    // Conversation operations
    Task<Conversation?> GetConversationAsync(string conversationId, string userId);
    Task<IEnumerable<Conversation>> GetConversationsAsync(string userId, int maxItems = 50, string? continuationToken = null);
    Task<Conversation> CreateConversationAsync(Conversation conversation);
    Task<Conversation> UpdateConversationAsync(Conversation conversation);
    Task DeleteConversationAsync(string conversationId, string userId);

    // Message operations
    Task<Message?> GetMessageAsync(string messageId, string conversationId);
    Task<IEnumerable<Message>> GetMessagesAsync(string conversationId, int maxItems = 100, string? continuationToken = null);
    Task<Message> SaveMessageAsync(Message message);
    Task<IEnumerable<Message>> SaveMessagesAsync(IEnumerable<Message> messages);
    Task DeleteMessageAsync(string messageId, string conversationId);

    // User preferences operations
    Task<UserPreferences?> GetUserPreferencesAsync(string userId);
    Task<UserPreferences> SaveUserPreferencesAsync(UserPreferences preferences);

    // Utility operations
    Task<int> GetConversationMessageCountAsync(string conversationId);
    Task<bool> ConversationExistsAsync(string conversationId, string userId);
}
