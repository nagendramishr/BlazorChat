# Azure Resource Setup Script for BlazorChat
# Usage: ./init-azure-resources.ps1 -ResourceGroupName "MyResourceGroup" -AccountName "MyCosmosName"

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$AccountName,

    [string]$Location = "eastus",
    [string]$DatabaseName = "BlazorChat"
)

# Login check
$azAccount = az account show 2>$null
if (-not $azAccount) {
    Write-Host "Please login to Azure first..."
    az login
}

# 1. Create Resource Group if not exists
Write-Host "Checking Resource Group '$ResourceGroupName'..."
$rgExists = az group show --name $ResourceGroupName 2>$null
if (-not $rgExists) {
    Write-Host "Creating Resource Group..."
    az group create --name $ResourceGroupName --location $Location
}

# 2. Create Cosmos DB Account if not exists
Write-Host "Checking Cosmos DB Account '$AccountName'..."
$accountExists = az cosmosdb show --name $AccountName --resource-group $ResourceGroupName 2>$null
if (-not $accountExists) {
    Write-Host "Creating Cosmos DB Account (this may take 10+ minutes)..."
    az cosmosdb create --name $AccountName --resource-group $ResourceGroupName --kind GlobalDocumentDB --locations regionName=$Location
}

# 3. Create Database
Write-Host "Checking Database '$DatabaseName'..."
$dbExists = az cosmosdb sql database show --account-name $AccountName --resource-group $ResourceGroupName --name $DatabaseName 2>$null
if (-not $dbExists) {
    Write-Host "Creating Database..."
    az cosmosdb sql database create --account-name $AccountName --resource-group $ResourceGroupName --name $DatabaseName
}

# Function to ensure container exists with correct PK
function Ensure-Container {
    param($containerName, $partitionKeyPath)
    
    Write-Host "Ensuring container '$containerName'..."
    $containerExists = az cosmosdb sql container show --account-name $AccountName --resource-group $ResourceGroupName --database-name $DatabaseName --name $containerName 2>$null
    
    if (-not $containerExists) {
        Write-Host "Creating container '$containerName'..."
        az cosmosdb sql container create --account-name $AccountName --resource-group $ResourceGroupName --database-name $DatabaseName `
            --name $containerName `
            --partition-key-path $partitionKeyPath `
            --throughput 400
    } else {
        Write-Host "Container '$containerName' already exists."
    }
}

# 4. Create Containers
# NOTE: Partition Keys are set to /id based on current code implementation
Ensure-Container -containerName "conversations" -partitionKeyPath "/id"
Ensure-Container -containerName "messages" -partitionKeyPath "/id"
Ensure-Container -containerName "preferences" -partitionKeyPath "/id"
Ensure-Container -containerName "organizations" -partitionKeyPath "/id"

Write-Host "Azure Resources initialization complete!"
Write-Host "---------------------------------------"
Write-Host "Next Steps:"
Write-Host "1. Update src/appsettings.json with your Cosmos DB endpoint/key if not already done."
Write-Host "2. For local dev, run: cd src; dotnet ef database update"
