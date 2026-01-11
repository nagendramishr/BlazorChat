using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace src.Services
{
    /// <summary>
    /// Interface for custom telemetry tracking
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Tracks when a user sends a message
        /// </summary>
        void TrackMessageSent(string userId, string conversationId, int messageLength);

        /// <summary>
        /// Tracks when an AI response is received
        /// </summary>
        void TrackMessageReceived(string conversationId, int responseLength, double responseTimeMs);

        /// <summary>
        /// Tracks AI response time metrics
        /// </summary>
        void TrackAIResponseTime(string conversationId, double durationMs, bool isStreaming);

        /// <summary>
        /// Tracks conversation metrics (length, token count)
        /// </summary>
        void TrackConversationMetrics(string conversationId, int messageCount, int estimatedTokens);

        /// <summary>
        /// Tracks exceptions with custom properties
        /// </summary>
        void TrackException(Exception exception, string userId, string? conversationId, IDictionary<string, string>? additionalProperties = null);

        /// <summary>
        /// Tracks custom events
        /// </summary>
        void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
    }

    /// <summary>
    /// Service for tracking custom telemetry to Application Insights
    /// </summary>
    public class TelemetryService : ITelemetryService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<TelemetryService> _logger;

        public TelemetryService(TelemetryClient telemetryClient, ILogger<TelemetryService> logger)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        /// <summary>
        /// Tracks when a user sends a message
        /// </summary>
        public void TrackMessageSent(string userId, string conversationId, int messageLength)
        {
            var properties = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "ConversationId", conversationId }
            };

            var metrics = new Dictionary<string, double>
            {
                { "MessageLength", messageLength }
            };

            _telemetryClient.TrackEvent("MessageSent", properties, metrics);
            _logger.LogDebug("Tracked MessageSent event for user {UserId}, length {Length}", userId, messageLength);
        }

        /// <summary>
        /// Tracks when an AI response is received
        /// </summary>
        public void TrackMessageReceived(string conversationId, int responseLength, double responseTimeMs)
        {
            var properties = new Dictionary<string, string>
            {
                { "ConversationId", conversationId }
            };

            var metrics = new Dictionary<string, double>
            {
                { "ResponseLength", responseLength },
                { "ResponseTimeMs", responseTimeMs }
            };

            _telemetryClient.TrackEvent("MessageReceived", properties, metrics);
            _logger.LogDebug("Tracked MessageReceived event for conversation {ConversationId}, time {Time}ms", 
                conversationId, responseTimeMs);
        }

        /// <summary>
        /// Tracks AI response time metrics
        /// </summary>
        public void TrackAIResponseTime(string conversationId, double durationMs, bool isStreaming)
        {
            var metricName = isStreaming ? "AI_Streaming_Response_Time" : "AI_Response_Time";

            _telemetryClient.GetMetric(metricName).TrackValue(durationMs);

            var properties = new Dictionary<string, string>
            {
                { "ConversationId", conversationId },
                { "IsStreaming", isStreaming.ToString() }
            };

            var metrics = new Dictionary<string, double>
            {
                { "DurationMs", durationMs }
            };

            _telemetryClient.TrackEvent("AIResponseTime", properties, metrics);
            
            _logger.LogInformation("AI response time: {Duration}ms (Streaming: {IsStreaming})", durationMs, isStreaming);
        }

        /// <summary>
        /// Tracks conversation metrics (length, token count)
        /// </summary>
        public void TrackConversationMetrics(string conversationId, int messageCount, int estimatedTokens)
        {
            _telemetryClient.GetMetric("Conversation_Length").TrackValue(messageCount);
            _telemetryClient.GetMetric("Token_Usage").TrackValue(estimatedTokens);

            var properties = new Dictionary<string, string>
            {
                { "ConversationId", conversationId }
            };

            var metrics = new Dictionary<string, double>
            {
                { "MessageCount", messageCount },
                { "EstimatedTokens", estimatedTokens }
            };

            _telemetryClient.TrackEvent("ConversationMetrics", properties, metrics);
            
            _logger.LogDebug("Tracked conversation metrics: {MessageCount} messages, ~{TokenCount} tokens", 
                messageCount, estimatedTokens);
        }

        /// <summary>
        /// Tracks exceptions with custom properties
        /// </summary>
        public void TrackException(Exception exception, string userId, string? conversationId, 
            IDictionary<string, string>? additionalProperties = null)
        {
            var properties = new Dictionary<string, string>
            {
                { "UserId", userId }
            };

            if (!string.IsNullOrEmpty(conversationId))
            {
                properties["ConversationId"] = conversationId;
            }

            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            var telemetry = new ExceptionTelemetry(exception);
            foreach (var prop in properties)
            {
                telemetry.Properties[prop.Key] = prop.Value;
            }

            _telemetryClient.TrackException(telemetry);
            
            _logger.LogError(exception, "Tracked exception for user {UserId}, conversation {ConversationId}", 
                userId, conversationId ?? "none");
        }

        /// <summary>
        /// Tracks custom events
        /// </summary>
        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, 
            IDictionary<string, double>? metrics = null)
        {
            _telemetryClient.TrackEvent(eventName, properties, metrics);
            _logger.LogDebug("Tracked custom event: {EventName}", eventName);
        }
    }
}
