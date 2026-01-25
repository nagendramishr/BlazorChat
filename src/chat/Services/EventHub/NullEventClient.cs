namespace src.Services.EventHub;

/// <summary>
/// No-op event client used when Event Hub is not configured.
/// Silently discards all events.
/// </summary>
public class NullEventClient : IEventClient
{
    public int Count => 0;

    public void StopTimer()
    {
        // No-op
    }

    public void SendData(string? value)
    {
        // Discard - Event Hub is not configured
    }

    public void SendData(ChatEvent eventData)
    {
        // Discard - Event Hub is not configured
    }
}
