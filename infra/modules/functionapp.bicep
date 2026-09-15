@description('Azure region for deployment')
param location string = resourceGroup().location

@description('Function App resource name')
param functionAppName string = 'func-cognidoc-ingestion-dev'

@description('Name of the storage account used by Function host and blob triggers')
param storageAccountName string

@description('Storage Blob Service Endpoint URL')
param blobEndpoint string

@description('Cosmos DB Document Endpoint URL')
param cosmosEndpoint string

@description('Azure OpenAI Endpoint URL')
param openAiEndpoint string

@description('Azure AI Search Endpoint URL')
param searchEndpoint string

@description('Service Bus Namespace Name')
param serviceBusNamespaceName string

// Dedicated Linux Consumption Plan
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${functionAppName}'
  location: location
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

// Function App with System-Assigned Identity & .NET 10 Isolated runtime
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        // Passwordless host storage using Managed Identity
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        // Pipeline endpoints matching Web API configuration
        {
          name: 'Storage__BlobEndpoint'
          value: blobEndpoint
        }
        {
          name: 'CosmosDb__Endpoint'
          value: cosmosEndpoint
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAiEndpoint
        }
        {
          name: 'AzureSearch__Endpoint'
          value: searchEndpoint
        }
        {
          name: 'ServiceBus__FullyQualifiedNamespace'
          value: '${serviceBusNamespaceName}.servicebus.windows.net'
        }
      ]
    }
  }
}

output id string = functionApp.id
output name string = functionApp.name
output principalId string = functionApp.identity.principalId
output defaultHostName string = functionApp.properties.defaultHostName
