# BlazorChat

A modern AI-powered chat application built with .NET 10 Blazor Server, Azure Cosmos DB, and Microsoft Foundry AI Agents.

## Features

- 🤖 **AI-Powered Conversations** - Integration with Microsoft Foundry AI agents for intelligent responses
  - **Custom Content** - Customize the knowledgebase that the LLM will use for generating responses
- 💬 **Real-time Chat** - Blazor Server with streaming responses
  - **Multi-Tenant** - Can host chats for multiple organizations, each with their own custom look and feel
- 📝 **Markdown Support** - Rich text formatting with code highlighting
- 💾 **Persistent Storage** - Azure Cosmos DB for conversations and messages
- 🔐 **Secure Authentication** - ASP.NET Core Identity with user management
  - **Microsoft Entra** - Integrate with Microsoft Entra ID for user authentication
  - **Custom Identity provider** - Extensible to use your own identity provider
- 📊 **Telemetry** - Application Insights integration for monitoring
- 🎨 **Modern UI** - MudBlazor component library with responsive design
- 🧠 **Context Management** - Smart token counting and conversation trimming
 
<img width="640" height="380" alt="image" src="https://github.com/user-attachments/assets/08154db0-fb02-4f1b-9606-f0770f0d20f5" />


## Prerequisites

- .NET 10 SDK
- Azure subscription with:
  - Azure Cosmos DB account
  - Microsoft Foundry (Azure AI Foundry) project with an AI agent
- Azure CLI (optional, for authentication)

## Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd BlazorChat
```

### 2. Configure Azure Authentication

The application uses `DefaultAzureCredential` for passwordless authentication. Choose one:

**Option A: Azure CLI**
```bash
az login
az account set --subscription <subscription-id>
```

**Option B: Visual Studio / VS Code**
- Sign in with your Azure account in the IDE

### 3. Configure Application Settings

Update `src/appsettings.json` with your Azure resources:

```json
{
  "CosmosDb": {
    "Endpoint": "https://your-account.documents.azure.com:443/",
    "DatabaseName": "BlazorChatDB",
    "ConversationsContainerName": "Conversations",
    "PreferencesContainerName": "UserPreferences"
  },
  "AIFoundry": {
    "Endpoint": "https://your-project.services.ai.azure.com/api/projects/your-project",
    "ExistingAgentId": "your-agent:1",
    "ModelDeployment": "gpt-4o",
    "AgentName": "BlazorChatAgent",
    "AgentInstructions": "You are a helpful AI assistant..."
  }
}
```

### 4. Set Up Azure Resources

**Cosmos DB:**
- Database: `BlazorChatDB` (or your chosen name)
- Containers:
  - `Conversations` (partition key: `/userId`)
  - `UserPreferences` (partition key: `/userId`)

**Microsoft Foundry:**
- Create an AI agent in Azure AI Foundry portal
- Note the agent ID (format: `name:version`)
- Deploy a model (e.g., gpt-4o)

### 5. Run the Application

```bash
cd src
dotnet restore
dotnet run
```

Navigate to: `https://localhost:5205` (or the port shown in terminal)

## Configuration Options

### Optional: Azure Key Vault

Store sensitive settings in Azure Key Vault:

```json
"KeyVault": {
  "VaultUri": "https://your-vault.vault.azure.net/"
}
```

### Optional: Application Insights

Enable telemetry and monitoring:

```json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=xxx;..."
}
```

### Development Settings

For local development, create `src/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "src.Services": "Debug"
    }
  }
}
```

## Project Structure

```
BlazorChat/
├── src/
│   ├── Components/
│   │   ├── Account/          # Authentication UI
│   │   ├── Layout/           # App layout and navigation
│   │   ├── Pages/            # Main pages (Chat, Home, etc.)
│   │   └── Shared/           # Reusable components (MarkdownRenderer)
│   ├── Data/                 # EF Core for Identity (SQLite)
│   ├── Models/               # Domain models (Conversation, Message)
│   ├── Services/             # Business logic
│   │   ├── AIFoundryService.cs           # AI agent communication
│   │   ├── CosmosDbService.cs            # Cosmos DB operations
│   │   ├── ConversationContextManager.cs # Token management
│   │   └── TelemetryService.cs           # Application Insights
│   └── Program.cs            # App startup and DI
├── deploy/                   # Infrastructure as Code (future)
├── instructions/             # Project documentation
└── README.md
```

## Key Technologies

- **Framework**: .NET 10 Blazor Server
- **UI**: MudBlazor 8.0
- **Database**: Azure Cosmos DB 3.45.0
- **AI**: Microsoft Foundry (Azure.AI.Projects, Microsoft.Agents.AI.AzureAI)
- **Authentication**: ASP.NET Core Identity
- **Monitoring**: Application Insights
- **Security**: Azure Key Vault, DefaultAzureCredential

## Common Issues

### Agent Not Found Error

Ensure your agent exists in Azure AI Foundry:
```bash
# Verify agent in portal or check ExistingAgentId format: "name:version"
```

### Cosmos DB Connection Issues

- Verify your Azure credentials: `az account show`
- Check Cosmos DB firewall allows your IP
- Ensure you have proper RBAC roles (Cosmos DB Data Contributor)

### Build Warnings

MudBlazor version resolution warning is expected and harmless (resolved to 8.0.0).

## Development

### Debug Locally

```bash
cd src
dotnet watch run
```

### Run Tests

```bash
dotnet test
```

### Database Migrations (Identity)

```bash
cd src
dotnet ef database update
```

## Production Deployment

See `instructions/tasks.txt` for:
- Docker containerization (Task 9.1)
- Azure deployment (Task 9.4)
- CI/CD pipeline setup (Task 9.3)

## Security Notes

- Never commit `appsettings.Development.json` (contains secrets)
- Use Azure Key Vault for production secrets
- Database files (`*.db`) are gitignored
- DefaultAzureCredential eliminates hardcoded credentials

## Contributing

See `instructions/priorities.txt` and `instructions/tasks.txt` for planned features and priorities.

## License

[Your License Here]

## Support

For issues or questions, please check:
- Azure AI Foundry documentation
- Azure Cosmos DB best practices
- Project issues/discussions


