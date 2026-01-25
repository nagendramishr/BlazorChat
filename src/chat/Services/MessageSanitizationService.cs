using System.Text.RegularExpressions;
using System.Web;

namespace src.Services;

/// <summary>
/// Service for sanitizing user input to prevent XSS and other injection attacks.
/// </summary>
public interface IMessageSanitizationService
{
    /// <summary>
    /// Sanitizes message content for safe storage and display.
    /// </summary>
    string SanitizeMessage(string content);

    /// <summary>
    /// Sanitizes a URL-safe slug.
    /// </summary>
    string SanitizeSlug(string slug);

    /// <summary>
    /// Validates and sanitizes custom CSS (limited set of safe properties).
    /// </summary>
    string SanitizeCss(string css);

    /// <summary>
    /// Checks if content contains potential prompt injection patterns.
    /// </summary>
    bool ContainsPromptInjection(string content);
}

/// <summary>
/// Implementation of message sanitization service.
/// </summary>
public partial class MessageSanitizationService : IMessageSanitizationService
{
    private readonly ILogger<MessageSanitizationService> _logger;

    // Patterns that may indicate prompt injection attempts
    private static readonly string[] PromptInjectionPatterns =
    [
        "ignore previous instructions",
        "ignore all previous",
        "disregard previous",
        "forget your instructions",
        "you are now",
        "act as if",
        "pretend you are",
        "system prompt",
        "new instructions:",
        "override:",
        "jailbreak"
    ];

    // Allowed CSS properties for organization theming
    private static readonly HashSet<string> AllowedCssProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "background-color", "font-family", "font-size", "font-weight",
        "border-color", "border-radius", "padding", "margin",
        "--mud-palette-primary", "--mud-palette-secondary", "--mud-palette-tertiary",
        "--mud-palette-background", "--mud-palette-surface"
    };

    public MessageSanitizationService(ILogger<MessageSanitizationService> logger)
    {
        _logger = logger;
    }

    public string SanitizeMessage(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Trim excessive whitespace
        content = content.Trim();

        // Limit length (defense in depth - validation should catch this first)
        if (content.Length > 32000)
        {
            _logger.LogWarning("Message content truncated from {OriginalLength} to 32000 characters", content.Length);
            content = content[..32000];
        }

        // Remove null bytes and other control characters (except newlines and tabs)
        content = ControlCharacterRegex().Replace(content, string.Empty);

        // Note: We do NOT HTML encode here because:
        // 1. Content goes to AI which needs raw text
        // 2. Blazor automatically encodes when rendering
        // 3. Markdown renderer handles code blocks safely

        return content;
    }

    public string SanitizeSlug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return string.Empty;

        // Convert to lowercase
        slug = slug.ToLowerInvariant();

        // Remove any character that isn't alphanumeric or hyphen
        slug = SlugRegex().Replace(slug, string.Empty);

        // Collapse multiple hyphens
        slug = MultipleHyphenRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Limit length
        if (slug.Length > 50)
            slug = slug[..50].TrimEnd('-');

        return slug;
    }

    public string SanitizeCss(string css)
    {
        if (string.IsNullOrEmpty(css))
            return string.Empty;

        var sanitizedLines = new List<string>();

        // Parse CSS line by line
        var lines = css.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("/*"))
                continue;

            // Check for dangerous patterns
            if (trimmedLine.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("expression(", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("url(", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("@import", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Blocked potentially dangerous CSS: {Css}", trimmedLine);
                continue;
            }

            // Extract property name
            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex > 0)
            {
                var property = trimmedLine[..colonIndex].Trim();
                if (AllowedCssProperties.Contains(property))
                {
                    sanitizedLines.Add(trimmedLine);
                }
                else
                {
                    _logger.LogDebug("Filtered non-whitelisted CSS property: {Property}", property);
                }
            }
        }

        return string.Join("\n", sanitizedLines);
    }

    public bool ContainsPromptInjection(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        var lowerContent = content.ToLowerInvariant();

        foreach (var pattern in PromptInjectionPatterns)
        {
            if (lowerContent.Contains(pattern))
            {
                _logger.LogWarning("Potential prompt injection detected: pattern '{Pattern}' found", pattern);
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex ControlCharacterRegex();

    [GeneratedRegex(@"[^a-z0-9-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphenRegex();
}
