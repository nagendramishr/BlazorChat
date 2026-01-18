# Deploy BlazorChat to Azure App Service with .NET 10.0
# Usage: .\deploy\deploy.ps1 -ResourceGroupName <rg-name> -Location <location>

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$false)]
    [string]$ParametersFile = "deploy/parameters.json"
)

$ErrorActionPreference = "Stop"

Write-Host "Starting deployment to Azure App Service with .NET 10.0..." -ForegroundColor Cyan

# Check if logged in to Azure
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Not logged in to Azure. Please login..." -ForegroundColor Red
    az login
}

# Create resource group if it doesn't exist
Write-Host "Ensuring resource group exists..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location

# Deploy infrastructure
Write-Host "Deploying Azure infrastructure..." -ForegroundColor Yellow
$deploymentOutput = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file deploy/main.bicep `
    --parameters @$ParametersFile `
    --query "properties.outputs" `
    --output json | ConvertFrom-Json

$appServiceName = $deploymentOutput.appServiceName.value
$appServiceUrl = $deploymentOutput.appServiceUrl.value
$principalId = $deploymentOutput.appServicePrincipalId.value

Write-Host "App Service created: $appServiceName" -ForegroundColor Green
Write-Host "App Service URL: $appServiceUrl" -ForegroundColor Green

# Configure .NET 10.0 runtime
Write-Host "Configuring .NET 10.0 runtime..." -ForegroundColor Yellow
az webapp config set `
    --resource-group $ResourceGroupName `
    --name $appServiceName `
    --linux-fx-version "DOTNETCORE|10.0"

# Build and publish the application
Write-Host "Building and publishing application..." -ForegroundColor Yellow
Push-Location $PSScriptRoot\..
dotnet publish ./src/src.csproj -c Release -o ./publish --self-contained false

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
if (Test-Path ./deploy.zip) {
    Remove-Item ./deploy.zip -Force
}
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

# Deploy to App Service
Write-Host "Deploying to App Service..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group $ResourceGroupName `
    --name $appServiceName `
    --src ./deploy.zip

# Clean up
Remove-Item ./deploy.zip -Force
Remove-Item ./publish -Recurse -Force

Pop-Location

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "App Service Name: $appServiceName" -ForegroundColor Cyan
Write-Host "App Service URL: $appServiceUrl" -ForegroundColor Cyan
Write-Host "Managed Identity Principal ID: $principalId" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Grant the Managed Identity access to Cosmos DB" -ForegroundColor White
Write-Host "2. Grant the Managed Identity access to Key Vault (if using)" -ForegroundColor White
Write-Host "3. Grant the Managed Identity access to AI Foundry" -ForegroundColor White
Write-Host "4. Update parameters.json with your actual resource values" -ForegroundColor White
