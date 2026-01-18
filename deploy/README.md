# Deployment Guide for BlazorChat

This guide explains how to deploy BlazorChat to Azure App Service with .NET 10.0 support.

## Prerequisites

- Azure CLI installed and logged in
- .NET 10.0 SDK installed
- Azure subscription with appropriate permissions

## Deployment Steps

### 1. Update Parameters

Edit `deploy/parameters.json` and replace the placeholder values with your actual Azure resource names:

```json
{
  "cosmosDbAccountName": {
    "value": "your-actual-cosmosdb-account"
  },
  "aiFoundryEndpoint": {
    "value": "https://your-foundry-endpoint.openai.azure.com/"
  },
  "applicationInsightsConnectionString": {
    "value": "your-app-insights-connection-string"
  },
  "keyVaultUri": {
    "value": "https://your-keyvault.vault.azure.net/"
  }
}
```

### 2. Run Deployment Script

#### Using PowerShell (Windows)

```powershell
.\deploy\deploy.ps1 -ResourceGroupName "blazorchat-rg" -Location "eastus"
```

#### Using Azure Developer CLI (azd)

```bash
azd init
azd up
```

### 3. Configure Managed Identity Permissions

After deployment, grant the App Service's Managed Identity access to:

#### Cosmos DB
```bash
# Get the App Service principal ID from deployment output
PRINCIPAL_ID="<principal-id-from-output>"
COSMOS_ACCOUNT="<your-cosmos-account>"
RG_NAME="<your-resource-group>"

# Assign Cosmos DB Built-in Data Contributor role
az cosmosdb sql role assignment create \
  --account-name $COSMOS_ACCOUNT \
  --resource-group $RG_NAME \
  --scope "/" \
  --principal-id $PRINCIPAL_ID \
  --role-definition-id "00000000-0000-0000-0000-000000000002"
```

#### Key Vault (if using)
```bash
az keyvault set-policy \
  --name <your-keyvault-name> \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

#### AI Foundry
Grant the Managed Identity appropriate permissions in your AI Foundry project.

## .NET 10.0 Configuration

The deployment automatically configures the App Service to use .NET 10.0 by setting:

- `linuxFxVersion: 'DOTNETCORE|10.0'` in the Bicep template
- Linux-based App Service Plan (required for .NET 10.0 preview)

## Troubleshooting

### Error: SDK does not support .NET 10.0

If you encounter this error during deployment:
1. Ensure your local machine has .NET 10.0 SDK installed
2. The App Service will use the .NET 10.0 runtime configured in Azure
3. Use `--self-contained false` when publishing to rely on Azure's runtime

### Health Check Failures

The App Service is configured with health check path `/health`. Ensure:
1. Health checks are implemented in your application
2. The endpoint returns 200 OK when healthy

### Managed Identity Issues

If the app can't access Azure resources:
1. Verify the Managed Identity is enabled (done automatically)
2. Check role assignments for Cosmos DB, Key Vault, and AI Foundry
3. Review App Service logs: `az webapp log tail --name <app-name> --resource-group <rg-name>`

## Monitoring

View application logs:
```bash
az webapp log tail --name <app-service-name> --resource-group <rg-name>
```

Access Application Insights in Azure Portal for detailed telemetry.

## Local Development

For local development with .NET 10.0:
1. Install .NET 10.0 SDK from https://dotnet.microsoft.com/download/dotnet/10.0
2. Update your PATH to prioritize .NET 10.0 SDK
3. Verify: `dotnet --version` should show 10.0.x
