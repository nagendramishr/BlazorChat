@description('The location for all resources')
param location string = resourceGroup().location

@description('The name of the App Service')
param appServiceName string = 'blazorchat-${uniqueString(resourceGroup().id)}'

@description('The name of the App Service Plan')
param appServicePlanName string = 'blazorchat-plan-${uniqueString(resourceGroup().id)}'

@description('The SKU of the App Service Plan')
@allowed([
  'F1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1v3'
  'P2v3'
  'P3v3'
])
param appServicePlanSku string = 'B1'

@description('Cosmos DB account name')
param cosmosDbAccountName string

@description('Cosmos DB database name')
param cosmosDbDatabaseName string = 'blazorchat'

@description('AI Foundry endpoint')
@secure()
param aiFoundryEndpoint string

@description('Application Insights connection string')
@secure()
param applicationInsightsConnectionString string = ''

@description('Key Vault URI')
param keyVaultUri string = ''

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'CosmosDb__Endpoint'
          value: 'https://${cosmosDbAccountName}.documents.azure.com:443/'
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: cosmosDbDatabaseName
        }
        {
          name: 'CosmosDb__ConversationsContainerName'
          value: 'conversations'
        }
        {
          name: 'CosmosDb__MessagesContainerName'
          value: 'messages'
        }
        {
          name: 'CosmosDb__PreferencesContainerName'
          value: 'preferences'
        }
        {
          name: 'AIFoundry__Endpoint'
          value: aiFoundryEndpoint
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: applicationInsightsConnectionString
        }
        {
          name: 'KeyVault__VaultUri'
          value: keyVaultUri
        }
      ]
    }
  }
}

output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePrincipalId string = appService.identity.principalId
