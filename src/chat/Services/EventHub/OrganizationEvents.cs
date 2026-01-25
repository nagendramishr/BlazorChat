namespace src.Services.EventHub;

/// <summary>
/// Base event type for all organization-related events sent to Event Hub.
/// The Reporting Service consumes these for audit trails and analytics.
/// </summary>
public class OrganizationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "OrganizationEvent";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OrganizationId { get; set; }
    public string? OrganizationSlug { get; set; }
    public string? OrganizationName { get; set; }
    public string? PerformedByUserId { get; set; }
}

/// <summary>
/// Event fired when a new organization is created/onboarded.
/// </summary>
public class OrganizationCreatedEvent : OrganizationEvent
{
    public OrganizationCreatedEvent() => EventType = "OrganizationCreated";
    public bool HasCustomBranding { get; set; }
    public bool HasCustomAIConfig { get; set; }
}

/// <summary>
/// Event fired when organization details are updated.
/// </summary>
public class OrganizationUpdatedEvent : OrganizationEvent
{
    public OrganizationUpdatedEvent() => EventType = "OrganizationUpdated";
    public string[] ChangedFields { get; set; } = Array.Empty<string>();
    public bool BrandingChanged { get; set; }
    public bool AIConfigChanged { get; set; }
}

/// <summary>
/// Event fired when an organization is disabled.
/// </summary>
public class OrganizationDisabledEvent : OrganizationEvent
{
    public OrganizationDisabledEvent() => EventType = "OrganizationDisabled";
    public string? Reason { get; set; }
}

/// <summary>
/// Event fired when an organization is re-enabled.
/// </summary>
public class OrganizationEnabledEvent : OrganizationEvent
{
    public OrganizationEnabledEvent() => EventType = "OrganizationEnabled";
}

/// <summary>
/// Event fired when organization slug is changed (impacts URLs).
/// </summary>
public class OrganizationSlugChangedEvent : OrganizationEvent
{
    public OrganizationSlugChangedEvent() => EventType = "OrganizationSlugChanged";
    public string? OldSlug { get; set; }
    public string? NewSlug { get; set; }
}

/// <summary>
/// Event fired when organization AI configuration is updated.
/// This may affect active chat sessions.
/// </summary>
public class OrganizationAIConfigChangedEvent : OrganizationEvent
{
    public OrganizationAIConfigChangedEvent() => EventType = "OrganizationAIConfigChanged";
    public bool EndpointChanged { get; set; }
    public bool ModelChanged { get; set; }
    public bool AgentChanged { get; set; }
    public bool SystemPromptChanged { get; set; }
}
