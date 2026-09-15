@description('Azure region for deployment')
param location string = resourceGroup().location

@description('Function App resource name')
param functionAppName string = 'func-cognidoc-ingestion-dev'

@description('Resource ID of the existing App Service Plan')
param appServicePlanId string

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

// Function App hosted on the shared Linux App Service Plan
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      alwaysOn: true
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
        // Pipeline endpoints
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
