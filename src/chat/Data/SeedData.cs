using Microsoft.AspNetCore.Identity;

namespace src.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Seed Roles
        string[] roleNames = { "GlobalAdmin", "User" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Seeded role: {Role}", roleName);
            }
        }

        // Check if we should seed test user (useful for development)
        // Default to true in Development if not specified
        var seedTestUser = Environment.GetEnvironmentVariable("SEED_TEST_USER") ?? "true";
        if (string.Equals(seedTestUser, "true", StringComparison.OrdinalIgnoreCase))
        {
            var testEmail = Environment.GetEnvironmentVariable("TEST_USER_EMAIL") ?? "test@example.com";
            var testPassword = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD") ?? "Test123!";

            await CreateTestUserAsync(userManager, logger, testEmail, testPassword, isAdmin: true);
        }
    }

    private static async Task CreateTestUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        bool isAdmin = false)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            logger.LogInformation("Test user {Email} already exists", email);
            
            // Ensure admin role if requested (idempotency)
            if (isAdmin && !await userManager.IsInRoleAsync(existingUser, "GlobalAdmin"))
            {
                await userManager.AddToRoleAsync(existingUser, "GlobalAdmin");
                logger.LogInformation("Added GlobalAdmin role to existing user {Email}", email);
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Skip email confirmation for test user
            OrganizationId = "org-contoso" // Default to Contoso for test user
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            logger.LogInformation("Test user {Email} created successfully", email);
            if (isAdmin)
            {
                await userManager.AddToRoleAsync(user, "GlobalAdmin");
                logger.LogInformation("Assigned GlobalAdmin role to {Email}", email);
            }
        }
        else
        {
            logger.LogError("Failed to create test user {Email}: {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Alternative: Create a test user directly without environment variables
    /// </summary>
    public static async Task CreateDefaultTestUserAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        const string testEmail = "test@example.com";
        const string testPassword = "Test123!";

        var existingUser = await userManager.FindByEmailAsync(testEmail);
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, testPassword);
            
            if (result.Succeeded)
            {
                logger.LogInformation("Default test user created: {Email} / {Password}", testEmail, testPassword);
            }
        }
    }
}
