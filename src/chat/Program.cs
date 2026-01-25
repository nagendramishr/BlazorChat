using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using src.Components;
using src.Components.Account;
using src.Data;
using src.Services;
using Microsoft.AspNetCore.Authorization;
using src.Authorization;
using src.HealthChecks;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration (optional - only if VaultUri is configured)
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri) && !keyVaultUri.Contains("<your-"))
{
    var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
}

// Add Application Insights (always register, connection string is optional)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    // Only set connection string if provided and valid
    if (!string.IsNullOrEmpty(appInsightsConnectionString) && !appInsightsConnectionString.Contains("<your-"))
    {
        options.ConnectionString = appInsightsConnectionString;
    }
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Add Cosmos DB Service (singleton pattern for connection pooling)
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

// Add AI Foundry Service (singleton pattern for agent connection pooling)
builder.Services.AddSingleton<IAIFoundryService, AIFoundryService>();

// Add AI Foundry Service Factory for multi-org AI configuration
builder.Services.AddSingleton<IAIFoundryServiceFactory, AIFoundryServiceFactory>();

// Add Conversation Context Manager
builder.Services.AddSingleton<IConversationContextManager, ConversationContextManager>();

// Add Telemetry Service
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// Add Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, ConversationAuthorizationHandler>();

// Add Organization Service (Scoped because it holds per-request state)
builder.Services.AddScoped<IOrganizationService, OrganizationService>();

// Add Organization Admin Service (Scoped)
builder.Services.AddScoped<IOrganizationAdminService, OrganizationAdminService>();

// Add Layout State Service (Scoped for per-circuit state)
builder.Services.AddScoped<ILayoutStateService, LayoutStateService>();

// Add Thread State Service (configurable: InMemory, CosmosDb, or Redis)
var threadStateProvider = builder.Configuration.GetValue<string>("ThreadState:Provider") ?? "InMemory";
switch (threadStateProvider.ToLowerInvariant())
{
    case "redis":
        // Requires: Microsoft.Extensions.Caching.StackExchangeRedis NuGet package
        // To enable Redis, run: dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
        // Then uncomment the following code:
        /*
        var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "BlazorChat:";
            });
            builder.Services.AddScoped<src.Services.Cache.IThreadStateService, src.Services.Cache.RedisThreadStateService>();
            break;
        }
        */
        // Fall back to in-memory until Redis package is added
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddScoped<src.Services.Cache.IThreadStateService, src.Services.Cache.InMemoryThreadStateService>();
        break;
        
    case "cosmosdb":
        builder.Services.AddScoped<src.Services.Cache.IThreadStateService, src.Services.Cache.CosmosDbThreadStateService>();
        break;
        
    case "inmemory":
    default:
        builder.Services.AddScoped<src.Services.Cache.IThreadStateService, src.Services.Cache.InMemoryThreadStateService>();
        break;
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustOwnConversation", policy =>
        policy.Requirements.Add(new ConversationOwnerRequirement()));

    options.AddPolicy("GlobalAdmin", policy => 
        policy.RequireRole("GlobalAdmin"));
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<CosmosDbHealthCheck>("cosmosdb", tags: new[] { "ready", "startup" })
    .AddCheck<AIFoundryHealthCheck>("aifoundry", tags: new[] { "ready" })
    .AddCheck("liveness", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application is alive"), tags: new[] { "live" });

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://localhost:5001" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Global rate limit - 100 requests per minute per user
    options.AddPolicy("GlobalLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // AI Chat rate limit - 20 requests per minute (more restrictive due to cost)
    options.AddPolicy("AILimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

// Add Message Sanitization Service
builder.Services.AddSingleton<IMessageSanitizationService, MessageSanitizationService>();

// Add Chat Application Service (orchestrates chat operations)
builder.Services.AddScoped<src.Services.Application.IChatApplicationService, src.Services.Application.ChatApplicationService>();

// Add Theme Service
builder.Services.AddScoped<IThemeService, ThemeService>();

var app = builder.Build();

// Seed test data in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        // Seed Organizations
        await src.Data.OrgSeeder.SeedAsync(scope.ServiceProvider);

        await SeedData.InitializeAsync(scope.ServiceProvider);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowConfiguredOrigins");

// Enable Rate Limiting
app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

// Map Health Check Endpoints
// /liveness - Basic app alive check (no dependencies)
app.MapHealthChecks("/liveness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// /startup - Dependencies initialized check (Cosmos DB must be ready)
app.MapHealthChecks("/startup", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("startup")
});

// /readiness - Ready to accept traffic (all services operational)
app.MapHealthChecks("/readiness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
