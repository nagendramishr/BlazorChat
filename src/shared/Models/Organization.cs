using System.Text.Json.Serialization;

namespace BlazorChat.Shared.Models;

public class Organization
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; } = false;

    [JsonPropertyName("publicThemeSettings")]
    public ThemeSettings PublicThemeSettings { get; set; } = new();

    [JsonPropertyName("privateAIConfig")]
    public AIConfig PrivateAIConfig { get; set; } = new();
}

public class ThemeSettings
{
    [JsonPropertyName("primaryColor")]
    public string PrimaryColor { get; set; } = "#594AE2"; // Default MudBlazor Primary

    [JsonPropertyName("secondaryColor")]
    public string SecondaryColor { get; set; } = "#fa541c";

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("customCss")]
    public string? CustomCss { get; set; }

    [JsonPropertyName("headerHtml")]
    public string? HeaderHtml { get; set; }

    [JsonPropertyName("footerHtml")]
    public string? FooterHtml { get; set; }
}

public class AIConfig
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("modelDeployment")]
    public string? ModelDeployment { get; set; }

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("agentInstructions")]
    public string? AgentInstructions { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }
}
