using BlazorChat.Shared.Models;
using src.Services;

namespace src.Data;

public static class OrgSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var cosmosDbService = serviceProvider.GetRequiredService<ICosmosDbService>();
        
        // 1. Create Default Organization (Contoso)
        var contoso = new Organization
        {
            Id = "org-contoso",
            Name = "Contoso Corp",
            Slug = "contoso",
            PublicThemeSettings = new ThemeSettings
            {
                PrimaryColor = "#0078D4", // Microsoft Blue
                SecondaryColor = "#2B88D8",
                LogoUrl = "https://img.icons8.com/color/96/microsoft.png",
                HeaderHtml = "<div class='d-flex align-center px-4 py-2' style='background: #0078D4; color: white;'><strong>Contoso Corp</strong></div>",
                FooterHtml = "<div class='text-center py-2' style='font-size: 0.8rem; color: #666;'>© 2026 Contoso Corp - Internal Use Only</div>"
            }
        };

        // 2. Create Second Organization (Fabrikam)
        var fabrikam = new Organization
        {
            Id = "org-fabrikam",
            Name = "Fabrikam Inc",
            Slug = "fabrikam",
            PublicThemeSettings = new ThemeSettings
            {
                PrimaryColor = "#FF5722", // Deep Orange
                SecondaryColor = "#FF8A65",
                LogoUrl = "https://img.icons8.com/color/96/company.png",
                HeaderHtml = "<div class='d-flex align-center justify-center px-4 py-2' style='background: #FF5722; color: white; border-bottom: 4px solid #333;'><strong>FABRIKAM INC</strong></div>",
                FooterHtml = "<div class='text-center py-2' style='background: #333; color: white;'>Powered by Fabrikam AI</div>"
            }
        };

        try {
            // Try to fetch first to avoid duplicate errors, or just try create
            var existing = await cosmosDbService.GetOrganizationBySlugAsync("contoso");
            if (existing == null) {
                await cosmosDbService.CreateOrganizationAsync(contoso);
                Console.WriteLine("Seeded Contoso organization");
            }

            existing = await cosmosDbService.GetOrganizationBySlugAsync("fabrikam");
            if (existing == null) {
                await cosmosDbService.CreateOrganizationAsync(fabrikam);
                Console.WriteLine("Seeded Fabrikam organization");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Seeding error: {ex.Message}");
        }
    }
}
