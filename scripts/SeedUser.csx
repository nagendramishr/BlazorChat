using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using src.Data;

// Simple standalone seeding script
Console.WriteLine("=== BlazorChat Test User Seeder ===");
Console.WriteLine();

var connectionString = "DataSource=Data/app.db;Cache=Shared";
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite(connectionString);

using var context = new ApplicationDbContext(optionsBuilder.Options);

// Ensure database is created
await context.Database.MigrateAsync();
Console.WriteLine("✓ Database migrations applied");

// Setup UserManager
var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
var passwordHasher = new PasswordHasher<ApplicationUser>();
var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
var lookupNormalizer = new UpperInvariantLookupNormalizer();
var errors = new IdentityErrorDescriber();

var userManager = new UserManager<ApplicationUser>(
    userStore,
    null,
    passwordHasher,
    userValidators,
    passwordValidators,
    lookupNormalizer,
    errors,
    null,
    null);

// Create test user
const string testEmail = "test@example.com";
const string testPassword = "Test123!";

var existingUser = await userManager.FindByEmailAsync(testEmail);
if (existingUser != null)
{
    Console.WriteLine($"⚠ Test user '{testEmail}' already exists");
    Console.WriteLine($"  User ID: {existingUser.Id}");
    Console.WriteLine($"  Email Confirmed: {existingUser.EmailConfirmed}");
}
else
{
    var user = new ApplicationUser
    {
        UserName = testEmail,
        Email = testEmail,
        EmailConfirmed = true // Skip email confirmation for test user
    };

    var result = await userManager.CreateAsync(user, testPassword);

    if (result.Succeeded)
    {
        Console.WriteLine("✓ Test user created successfully!");
        Console.WriteLine();
        Console.WriteLine("Login Credentials:");
        Console.WriteLine($"  Email:    {testEmail}");
        Console.WriteLine($"  Password: {testPassword}");
        Console.WriteLine($"  User ID:  {user.Id}");
    }
    else
    {
        Console.WriteLine("✗ Failed to create test user:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"  - {error.Description}");
        }
    }
}

Console.WriteLine();
Console.WriteLine("Done!");
