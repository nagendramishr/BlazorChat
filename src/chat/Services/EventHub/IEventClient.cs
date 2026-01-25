namespace src.Services.EventHub;

/// <summary>
/// Interface for publishing events to an event sink.
/// Adapted from SimpleL7Proxy.
/// </summary>
public interface IEventClient
{
    int Count { get; }
    void StopTimer();
    void SendData(string? value);
    void SendData(ChatEvent eventData);
    void SendData(OrganizationEvent eventData);
}
