using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace src.Services;

/// <summary>
/// Service for interacting with Microsoft Foundry AI agents.
/// Uses Microsoft Agent Framework for agent communication with streaming support.
/// </summary>
public class AIFoundryService : IAIFoundryService, IAsyncDisposable
{
    private readonly ILogger<AIFoundryService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConversationContextManager _contextManager;
    private readonly ITelemetryService? _telemetryService;
    private AIProjectClient? _aiProjectClient;
    private AIAgent? _agent;
    private bool _agentWasCreatedByService; // Track if we created the agent vs retrieved existing
    private readonly ConcurrentDictionary<string, (AgentThread Thread, ThreadInfo Info)> _conversationThreads = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    // Thread expiry configuration (default: 24 hours)
    private static readonly TimeSpan DefaultThreadExpiry = TimeSpan.FromHours(24);

    public AIFoundryService(
        ILogger<AIFoundryService> logger,
        IConfiguration configuration,
        IConversationContextManager contextManager,
        ITelemetryService? telemetryService = null)
    {
        _logger = logger;
        _configuration = configuration;
        _contextManager = contextManager;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Initializes the AI agent connection to Microsoft Foundry.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            _logger.LogInformation("Initializing Microsoft Foundry AI agent...");

            var endpoint = _configuration["AIFoundry:Endpoint"];
            var existingAgentId = _configuration["AIFoundry:ExistingAgentId"];
            var modelDeployment = _configuration["AIFoundry:ModelDeployment"];
            var agentName = _configuration["AIFoundry:AgentName"] ?? "BlazorChatAgent";
            var agentInstructions = _configuration["AIFoundry:AgentInstructions"] 
                ?? "You are a helpful AI assistant.";

            if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Contains("<your-"))
            {
                throw new InvalidOperationException(
                    "Microsoft Foundry endpoint not configured. Please update appsettings.json with your project endpoint.");
            }

            // Create AI Project Client with DefaultAzureCredential
            _aiProjectClient = new AIProjectClient(
                new Uri(endpoint),
                new DefaultAzureCredential());

            // Check if we should use an existing agent or create a new one
            if (!string.IsNullOrWhiteSpace(existingAgentId) && !existingAgentId.Contains("<your-"))
            {
                try
                {
                    // Parse agent ID format: "name:version" or just "name"
                    string agentNameForRetrieval;
                    string? agentVersionForRetrieval = null;
                    
                    if (existingAgentId.Contains(':'))
                    {
                        var parts = existingAgentId.Split(':', 2);
                        agentNameForRetrieval = parts[0];
                        agentVersionForRetrieval = parts.Length > 1 ? parts[1] : null;
                    }
                    else
                    {
                        agentNameForRetrieval = existingAgentId;
                    }
                    
                    _logger.LogInformation("Attempting to retrieve existing agent: {AgentName}, Version: {Version}", 
                        agentNameForRetrieval, agentVersionForRetrieval ?? "latest");
                    
                    // Get the agent version
                    if (!string.IsNullOrWhiteSpace(agentVersionForRetrieval))
                    {
                        _logger.LogInformation("Retrieving agent: {Name}, version: {Version}", 
                            agentNameForRetrieval, agentVersionForRetrieval);
                        
                        var agentVersionResult = await _aiProjectClient.Agents.GetAgentVersionAsync(
                            agentNameForRetrieval, 
                            agentVersionForRetrieval,
                            cancellationToken);
                        
                        var agentVersion = agentVersionResult.Value;
                        
                        _logger.LogInformation("Agent version retrieved: {Name}:{Version}", 
                            agentVersion.Name, agentVersion.Version);
                        
                        _agent = _aiProjectClient.GetAIAgent(agentVersion);
                    }
                    else
                    {
                        // Get latest version by name only
                        _logger.LogInformation("Retrieving latest version of agent: {Name}", agentNameForRetrieval);
                        _agent = _aiProjectClient.GetAIAgent(agentNameForRetrieval);
                    }
                    
                    _agentWasCreatedByService = false; // We retrieved an existing agent
                    _logger.LogInformation(
                        "Microsoft Foundry AI agent retrieved successfully. Agent: {AgentName}",
                        agentNameForRetrieval);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to retrieve existing agent '{AgentId}'. Error: {Message}", 
                        existingAgentId, ex.Message);
                    
                    throw new InvalidOperationException(
                        $"Failed to retrieve existing agent '{existingAgentId}'. " +
                        $"Please verify the agent exists in your project or remove ExistingAgentId to create a new agent. " +
                        $"Error: {ex.Message}", ex);
                }
            }
            else
            {
                // Create a new agent
                if (string.IsNullOrWhiteSpace(modelDeployment) || modelDeployment.Contains("<your-"))
                {
                    throw new InvalidOperationException(
                        "Microsoft Foundry model deployment not configured. Please update appsettings.json with your model deployment name.");
                }

                _logger.LogInformation("Creating new agent: {AgentName} with model: {Model}", agentName, modelDeployment);
                
                _agent = await _aiProjectClient.CreateAIAgentAsync(
                    name: agentName,
                    model: modelDeployment,
                    instructions: agentInstructions,
                    cancellationToken: cancellationToken);

                _agentWasCreatedByService = true; // We created this agent
                _logger.LogInformation(
                    "Microsoft Foundry AI agent created successfully. Agent: {AgentName}, Model: {Model}",
                    agentName, modelDeployment);
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Microsoft Foundry AI agent");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Sends a message and streams the response back.
    /// </summary>
    public async IAsyncEnumerable<string> SendMessageStreamingAsync(
        string conversationId,
        string message,
        string? existingThreadId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_agent == null)
            throw new InvalidOperationException("AI agent is not initialized");

        List<string> responseChunks;
        var startTime = DateTime.UtcNow;

        try
        {
            // Get or create thread for this conversation
            var (thread, _) = await GetOrCreateThreadAsync(conversationId, existingThreadId, cancellationToken);

            _logger.LogInformation(
                "Sending message to AI agent. ConversationId: {ConversationId}, MessageLength: {Length}",
                conversationId, message.Length);

            responseChunks = new List<string>();

            // Stream the response - collect chunks first to avoid yield in try-catch
            await foreach (var update in _agent.RunStreamingAsync(message, thread, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    responseChunks.Add(update.Text);
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var totalLength = responseChunks.Sum(c => c.Length);

            _logger.LogInformation(
                "AI agent response completed. ConversationId: {ConversationId}, Duration: {Duration}ms, Length: {Length}",
                conversationId, duration, totalLength);

            // Track telemetry
            _telemetryService?.TrackAIResponseTime(conversationId, duration, isStreaming: true);
            _telemetryService?.TrackMessageReceived(conversationId, totalLength, duration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "AI agent request cancelled. ConversationId: {ConversationId}",
                conversationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending message to AI agent. ConversationId: {ConversationId}",
                conversationId);
            throw;
        }

        // Yield collected chunks outside of try-catch
        foreach (var chunk in responseChunks)
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Sends a message and returns the complete response.
    /// </summary>
    public async Task<string> SendMessageAsync(
        string conversationId,
        string message,
        string? existingThreadId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_agent == null)
            throw new InvalidOperationException("AI agent is not initialized");

        var startTime = DateTime.UtcNow;

        try
        {
            // Get or create thread for this conversation
            var (thread, _) = await GetOrCreateThreadAsync(conversationId, existingThreadId, cancellationToken);

            _logger.LogInformation(
                "Sending message to AI agent (non-streaming). ConversationId: {ConversationId}",
                conversationId);

            // Get complete response
            var response = await _agent.RunAsync(message, thread, cancellationToken: cancellationToken);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var responseLength = response?.Text?.Length ?? 0;

            _logger.LogInformation(
                "AI agent response completed. ConversationId: {ConversationId}, Duration: {Duration}ms, ResponseLength: {Length}",
                conversationId, duration, responseLength);

            // Track telemetry
            _telemetryService?.TrackAIResponseTime(conversationId, duration, isStreaming: false);
            _telemetryService?.TrackMessageReceived(conversationId, responseLength, duration);

            return response?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending message to AI agent. ConversationId: {ConversationId}",
                conversationId);
            throw;
        }
    }

    /// <summary>
    /// Clears the conversation thread for a conversation.
    /// </summary>
    public Task ClearConversationAsync(string conversationId)
    {
        if (_conversationThreads.TryRemove(conversationId, out _))
        {
            _logger.LogInformation("Cleared conversation thread. ConversationId: {ConversationId}", conversationId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current thread info for a conversation.
    /// </summary>
    public ThreadInfo? GetThreadInfo(string conversationId)
    {
        if (_conversationThreads.TryGetValue(conversationId, out var entry))
        {
            return entry.Info;
        }
        return null;
    }

    /// <summary>
    /// Gets or creates a thread for a conversation.
    /// Note: The Microsoft.Agents.AI SDK doesn't support thread resume by ID.
    /// Threads are in-memory only. The existingThreadId is used to check if we
    /// have a cached thread for this conversation.
    /// </summary>
    private Task<(AgentThread Thread, ThreadInfo Info)> GetOrCreateThreadAsync(
        string conversationId,
        string? existingThreadId,
        CancellationToken cancellationToken)
    {
        // Check if we already have the thread in memory
        if (_conversationThreads.TryGetValue(conversationId, out var existing))
        {
            // Check if thread is still valid (not expired)
            if (existing.Info.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Using cached thread for conversation {ConversationId}", conversationId);
                return Task.FromResult(existing);
            }
            else
            {
                _logger.LogInformation("Thread expired for conversation {ConversationId}, creating new thread", conversationId);
                _conversationThreads.TryRemove(conversationId, out _);
            }
        }

        // Note: Microsoft.Agents.AI SDK does not support resuming threads by ID.
        // If existingThreadId is provided but we don't have it in memory, we must create a new thread.
        // The AI agent will not have previous context - this is a limitation of the SDK.
        if (!string.IsNullOrWhiteSpace(existingThreadId))
        {
            _logger.LogInformation(
                "Thread {ThreadId} not in memory cache for conversation {ConversationId}. Creating new thread (context will be lost).",
                existingThreadId, conversationId);
        }

        // Create new thread
        var newThread = _agent!.GetNewThread();
        var createdAt = DateTime.UtcNow;
        
        // Generate a unique ID for tracking (since AgentThread doesn't expose Id)
        var threadId = $"thread_{conversationId}_{createdAt:yyyyMMddHHmmss}";
        
        var newInfo = new ThreadInfo
        {
            ThreadId = threadId,
            CreatedAt = createdAt,
            ExpiresAt = createdAt.Add(DefaultThreadExpiry),
            IsNewThread = true
        };

        var newEntry = (newThread, newInfo);
        _conversationThreads[conversationId] = newEntry;

        _logger.LogInformation("Created new thread {ThreadId} for conversation {ConversationId}",
            threadId, conversationId);

        return Task.FromResult(newEntry);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing AIFoundryService...");

        // Clear all conversation threads
        _conversationThreads.Clear();

        // Only delete the agent if WE created it (not if we retrieved an existing one)
        if (_agent != null && _aiProjectClient != null && _agentWasCreatedByService)
        {
            try
            {
                _logger.LogInformation("Deleting agent created by service: {AgentName}", _agent.Name);
                await _aiProjectClient.Agents.DeleteAgentAsync(_agent.Name);
                _logger.LogInformation("AI agent deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting AI agent during disposal");
            }
        }
        else if (_agent != null && !_agentWasCreatedByService)
        {
            _logger.LogInformation("Skipping deletion of existing agent: {AgentName} (not created by service)", _agent.Name);
        }

        _initLock.Dispose();
    }
}
