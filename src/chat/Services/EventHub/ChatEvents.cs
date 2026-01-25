namespace src.Services.EventHub;

/// <summary>
/// Base event type for all chat-related events sent to Event Hub.
/// The Reporting Service consumes these for usage tracking, analytics, and quotas.
/// </summary>
public class ChatEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "ChatEvent";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OrganizationId { get; set; }
    public string? UserId { get; set; }
    public string? ConversationId { get; set; }
    public string? MessageId { get; set; }
}

/// <summary>
/// Event fired when a message is sent to the AI.
/// </summary>
public class MessageSentEvent : ChatEvent
{
    public MessageSentEvent() => EventType = "MessageSent";
    public int InputCharacters { get; set; }
    public bool IsNewConversation { get; set; }
}

/// <summary>
/// Event fired when an AI response is received.
/// </summary>
public class MessageReceivedEvent : ChatEvent
{
    public MessageReceivedEvent() => EventType = "MessageReceived";
    public long DurationMs { get; set; }
    public bool IsStreaming { get; set; }
    public int OutputCharacters { get; set; }
    public string? Model { get; set; }
}

/// <summary>
/// Event fired when a conversation is created.
/// </summary>
public class ConversationCreatedEvent : ChatEvent
{
    public ConversationCreatedEvent() => EventType = "ConversationCreated";
    public string? Title { get; set; }
}

/// <summary>
/// Event fired when a conversation is deleted.
/// </summary>
public class ConversationDeletedEvent : ChatEvent
{
    public ConversationDeletedEvent() => EventType = "ConversationDeleted";
    public int MessageCount { get; set; }
}

/// <summary>
/// Event fired when an AI thread is resumed.
/// </summary>
public class ThreadResumedEvent : ChatEvent
{
    public ThreadResumedEvent() => EventType = "ThreadResumed";
    public string? AgentThreadId { get; set; }
    public bool WasCached { get; set; }
    public int MessagesReplayed { get; set; }
}

/// <summary>
/// Event fired when a quota limit is reached.
/// </summary>
public class QuotaExceededEvent : ChatEvent
{
    public QuotaExceededEvent() => EventType = "QuotaExceeded";
    public string? QuotaType { get; set; }
    public int CurrentUsage { get; set; }
    public int Limit { get; set; }
}

/// <summary>
/// Event fired when an error occurs during AI processing.
/// </summary>
public class ChatErrorEvent : ChatEvent
{
    public ChatErrorEvent() => EventType = "ChatError";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRecoverable { get; set; }
}
