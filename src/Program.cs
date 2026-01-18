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
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Add Cosmos DB Service (singleton pattern for connection pooling)
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

// Add AI Foundry Service (singleton pattern for agent connection pooling)
builder.Services.AddSingleton<IAIFoundryService, AIFoundryService>();

// Add Conversation Context Manager
builder.Services.AddSingleton<IConversationContextManager, ConversationContextManager>();

// Add Telemetry Service
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// Add Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, ConversationAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustOwnConversation", policy =>
        policy.Requirements.Add(new ConversationOwnerRequirement()));
});

var app = builder.Build();

// Seed test data in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
