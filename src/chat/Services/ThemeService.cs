using BlazorChat.Shared.Models;
using MudBlazor;

namespace src.Services;

/// <summary>
/// Service for resolving and applying organization-based themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the MudBlazor theme for an organization.
    /// </summary>
    MudTheme GetThemeForOrganization(Organization? organization);

    /// <summary>
    /// Gets custom CSS for an organization.
    /// </summary>
    string GetCustomCss(Organization? organization);

    /// <summary>
    /// Gets the default theme when no organization is specified.
    /// </summary>
    MudTheme GetDefaultTheme();
}

/// <summary>
/// Implementation of theme service for organization-based theming.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private readonly IMessageSanitizationService _sanitizationService;
    private readonly MudTheme _defaultTheme;

    public ThemeService(
        ILogger<ThemeService> logger,
        IMessageSanitizationService sanitizationService)
    {
        _logger = logger;
        _sanitizationService = sanitizationService;

        // Create default theme
        _defaultTheme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1976d2",
                Secondary = "#dc004e",
                AppbarBackground = "#1976d2",
                Background = "#f5f5f5",
                Surface = "#ffffff",
                DrawerBackground = "#ffffff",
                DrawerText = "rgba(0,0,0,0.87)",
                DrawerIcon = "rgba(0,0,0,0.54)"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#90caf9",
                Secondary = "#f48fb1",
                AppbarBackground = "#1e1e1e",
                Background = "#121212",
                Surface = "#1e1e1e",
                DrawerBackground = "#1e1e1e",
                DrawerText = "rgba(255,255,255,0.87)",
                DrawerIcon = "rgba(255,255,255,0.54)"
            }
        };
    }

    public MudTheme GetThemeForOrganization(Organization? organization)
    {
        if (organization == null)
        {
            _logger.LogDebug("No organization provided, using default theme");
            return _defaultTheme;
        }

        try
        {
            var themeSettings = organization.PublicThemeSettings;
            
            // Create a custom theme based on organization settings
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Primary = ParseColor(themeSettings.PrimaryColor, "#1976d2"),
                    Secondary = ParseColor(themeSettings.SecondaryColor, "#dc004e"),
                    AppbarBackground = ParseColor(themeSettings.PrimaryColor, "#1976d2"),
                    Background = "#f5f5f5",
                    Surface = "#ffffff",
                    DrawerBackground = "#ffffff",
                    DrawerText = "rgba(0,0,0,0.87)",
                    DrawerIcon = "rgba(0,0,0,0.54)"
                },
                PaletteDark = new PaletteDark
                {
                    Primary = LightenColor(themeSettings.PrimaryColor, "#90caf9"),
                    Secondary = LightenColor(themeSettings.SecondaryColor, "#f48fb1"),
                    AppbarBackground = "#1e1e1e",
                    Background = "#121212",
                    Surface = "#1e1e1e",
                    DrawerBackground = "#1e1e1e",
                    DrawerText = "rgba(255,255,255,0.87)",
                    DrawerIcon = "rgba(255,255,255,0.54)"
                }
            };

            _logger.LogDebug("Created custom theme for organization {OrgName}", organization.Name);
            return theme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating theme for organization {OrgId}, using default", organization.Id);
            return _defaultTheme;
        }
    }

    public string GetCustomCss(Organization? organization)
    {
        if (organization == null || string.IsNullOrEmpty(organization.PublicThemeSettings.CustomCss))
            return string.Empty;

        // Sanitize the custom CSS to prevent XSS
        return _sanitizationService.SanitizeCss(organization.PublicThemeSettings.CustomCss);
    }

    public MudTheme GetDefaultTheme()
    {
        return _defaultTheme;
    }

    private static string ParseColor(string? color, string defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;

        // Validate hex color format
        if (System.Text.RegularExpressions.Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"))
            return color;

        return defaultColor;
    }

    private static string LightenColor(string? color, string defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;

        try
        {
            // Simple lightening: blend with white
            if (!color.StartsWith('#') || color.Length != 7)
                return defaultColor;

            var r = Convert.ToInt32(color.Substring(1, 2), 16);
            var g = Convert.ToInt32(color.Substring(3, 2), 16);
            var b = Convert.ToInt32(color.Substring(5, 2), 16);

            // Lighten by 40%
            r = Math.Min(255, r + (255 - r) * 40 / 100);
            g = Math.Min(255, g + (255 - g) * 40 / 100);
            b = Math.Min(255, b + (255 - b) * 40 / 100);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return defaultColor;
        }
    }
}
