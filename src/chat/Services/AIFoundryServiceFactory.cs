using BlazorChat.Shared.Models;
using Microsoft.Extensions.Primitives;

namespace src.Services;

/// <summary>
/// Factory for creating organization-specific AI service instances.
/// Enables multi-tenant AI configurations where each organization can have its own AI agent settings.
/// </summary>
public interface IAIFoundryServiceFactory
{
    /// <summary>
    /// Gets or creates an AI service instance for a specific organization.
    /// </summary>
    /// <param name="organization">The organization to get AI service for. If null, uses global config.</param>
    /// <returns>An AI service instance configured for the organization.</returns>
    Task<IAIFoundryService> GetServiceAsync(Organization? organization);

    /// <summary>
    /// Gets the global AI service instance (uses appsettings.json config).
    /// </summary>
    Task<IAIFoundryService> GetGlobalServiceAsync();
}

/// <summary>
/// Implementation of AI Foundry service factory.
/// Manages a pool of AI service instances for different organizations.
/// </summary>
public class AIFoundryServiceFactory : IAIFoundryServiceFactory, IAsyncDisposable
{
    private readonly ILogger<AIFoundryServiceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAIFoundryService _globalService;
    private readonly Dictionary<string, IAIFoundryService> _orgServices = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AIFoundryServiceFactory(
        ILogger<AIFoundryServiceFactory> logger,
        IServiceProvider serviceProvider,
        IAIFoundryService globalService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _globalService = globalService;
    }

    public async Task<IAIFoundryService> GetServiceAsync(Organization? organization)
    {
        // If no organization or org has no custom AI config, use global service
        if (organization == null || 
            organization.PrivateAIConfig == null ||
            string.IsNullOrEmpty(organization.PrivateAIConfig.Endpoint))
        {
            return await GetGlobalServiceAsync();
        }

        var orgId = organization.Id;

        await _lock.WaitAsync();
        try
        {
            // Check if we already have a service for this org
            if (_orgServices.TryGetValue(orgId, out var existingService))
            {
                return existingService;
            }

            // Create a new service for this organization
            _logger.LogInformation("Creating AI service for organization {OrgId} with custom endpoint", orgId);

            var orgService = CreateOrganizationService(organization);
            await orgService.InitializeAsync();

            _orgServices[orgId] = orgService;
            return orgService;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IAIFoundryService> GetGlobalServiceAsync()
    {
        await _globalService.InitializeAsync();
        return _globalService;
    }

    private AIFoundryService CreateOrganizationService(Organization organization)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<AIFoundryService>>();
        var contextManager = _serviceProvider.GetRequiredService<IConversationContextManager>();
        var telemetryService = _serviceProvider.GetService<ITelemetryService>();

        // Create a custom configuration that overlays org settings
        var orgConfig = new OrganizationAIConfiguration(organization.PrivateAIConfig);

        return new AIFoundryService(logger, orgConfig, contextManager, telemetryService);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var service in _orgServices.Values)
        {
            if (service is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
        _orgServices.Clear();

        if (_globalService is IAsyncDisposable globalDisposable)
        {
            await globalDisposable.DisposeAsync();
        }
    }
}

/// <summary>
/// Configuration adapter that wraps organization AI config as IConfiguration.
/// </summary>
internal class OrganizationAIConfiguration : IConfiguration
{
    private readonly AIConfig _aiConfig;
    private readonly Dictionary<string, string?> _values;

    public OrganizationAIConfiguration(AIConfig aiConfig)
    {
        _aiConfig = aiConfig;
        _values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AIFoundry:Endpoint"] = aiConfig.Endpoint,
            ["AIFoundry:ModelDeployment"] = aiConfig.ModelDeployment,
            ["AIFoundry:AgentName"] = aiConfig.AgentName,
            ["AIFoundry:AgentInstructions"] = aiConfig.AgentInstructions ?? aiConfig.SystemPrompt
        };
    }

    public string? this[string key]
    {
        get => _values.TryGetValue(key, out var value) ? value : null;
        set => _values[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken() => new Microsoft.Extensions.Primitives.CancellationChangeToken(CancellationToken.None);

    public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

    private class ConfigurationSection : IConfigurationSection
    {
        private readonly OrganizationAIConfiguration _parent;
        private readonly string _key;

        public ConfigurationSection(OrganizationAIConfiguration parent, string key)
        {
            _parent = parent;
            _key = key;
        }

        public string? this[string key]
        {
            get => _parent[$"{_key}:{key}"];
            set => _parent[$"{_key}:{key}"] = value;
        }

        public string Key => _key.Contains(':') ? _key.Split(':').Last() : _key;
        public string Path => _key;
        public string? Value
        {
            get => _parent[_key];
            set => _parent[_key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => _parent.GetReloadToken();
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(_parent, $"{_key}:{key}");
    }
}
