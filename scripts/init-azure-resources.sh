#!/bin/bash

# Azure Resource Setup Script for BlazorChat
# Usage: ./init-azure-resources.sh -g "MyResourceGroup" -n "MyCosmosName"

# Default values
LOCATION="eastus"
DATABASE_NAME="BlazorChat"

# Function to display usage
usage() {
    echo "Usage: $0 -g <ResourceGroupName> -n <AccountName> [-l <Location>] [-d <DatabaseName>]"
    exit 1
}

# Parse arguments
while getopts ":g:n:l:d:" opt; do
    case $opt in
        g) RESOURCE_GROUP="$OPTARG" ;;
        n) ACCOUNT_NAME="$OPTARG" ;;
        l) LOCATION="$OPTARG" ;;
        d) DATABASE_NAME="$OPTARG" ;;
        \?) echo "Invalid option -$OPTARG" >&2; usage ;;
    esac
done

# Validate mandatory arguments
if [ -z "$RESOURCE_GROUP" ] || [ -z "$ACCOUNT_NAME" ]; then
    usage
fi

# Login check
if ! az account show > /dev/null 2>&1; then
    echo "Please login to Azure first..."
    az login
fi

# 1. Create Resource Group if not exists
echo "Checking Resource Group '$RESOURCE_GROUP'..."
if ! az group show --name "$RESOURCE_GROUP" > /dev/null 2>&1; then
    echo "Creating Resource Group..."
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
    echo "Resource Group '$RESOURCE_GROUP' already exists."
fi

# 2. Create Cosmos DB Account if not exists
echo "Checking Cosmos DB Account '$ACCOUNT_NAME'..."
if ! az cosmosdb show --name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" > /dev/null 2>&1; then
    echo "Creating Cosmos DB Account (this may take 10+ minutes)..."
    az cosmosdb create --name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" --kind GlobalDocumentDB --locations regionName="$LOCATION"
else
    echo "Cosmos DB Account '$ACCOUNT_NAME' already exists."
fi

# 3. Create Database
echo "Checking Database '$DATABASE_NAME'..."
if ! az cosmosdb sql database show --account-name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" --name "$DATABASE_NAME" > /dev/null 2>&1; then
    echo "Creating Database..."
    az cosmosdb sql database create --account-name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" --name "$DATABASE_NAME"
else
    echo "Database '$DATABASE_NAME' already exists."
fi

# Function to ensure container exists with correct PK
ensure_container() {
    local container_name=$1
    local partition_key_path=$2
    
    echo "Ensuring container '$container_name'..."
    if ! az cosmosdb sql container show --account-name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" --database-name "$DATABASE_NAME" --name "$container_name" > /dev/null 2>&1; then
        echo "Creating container '$container_name'..."
        az cosmosdb sql container create --account-name "$ACCOUNT_NAME" --resource-group "$RESOURCE_GROUP" --database-name "$DATABASE_NAME" \
            --name "$container_name" \
            --partition-key-path "$partition_key_path" \
            --throughput 400
    else
        echo "Container '$container_name' already exists."
    fi
}

# 4. Create Containers
# NOTE: Partition Keys are set to /id based on current code implementation
ensure_container "conversations" "/id"
ensure_container "messages" "/id"
ensure_container "preferences" "/id"
ensure_container "organizations" "/id"

echo "Azure Resources initialization complete!"
echo "---------------------------------------"
echo "Next Steps:"
echo "1. Update src/appsettings.json with your Cosmos DB endpoint/key if not already done."
echo "2. For local dev, run: cd src; dotnet ef database update"
