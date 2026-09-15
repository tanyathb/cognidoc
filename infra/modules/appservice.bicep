param appName string = 'app-cognidoc-api-dev'
param location string = resourceGroup().location
param planSku string = 'B1'

param blobEndpoint string
param cosmosEndpoint string
param searchEndpoint string
param openAiEndpoint string
param serviceBusEndpoint string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-cognidoc-dev'
  location: location
  kind: 'linux'
  sku: {
    name: planSku
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'Storage__BlobEndpoint'
          value: blobEndpoint
        }
        {
          name: 'CosmosDb__Endpoint'
          value: cosmosEndpoint
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'CogniDocDb'
        }
        {
          name: 'CosmosDb__ContainerName'
          value: 'Documents'
        }
        {
          name: 'SearchServiceEndpoint'
          value: searchEndpoint
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAiEndpoint
        }
        {
          name: 'AzureOpenAI__ChatDeploymentName'
          value: 'gpt-5-mini'
        }
        {
          name: 'AzureOpenAI__EmbeddingDeploymentName'
          value: 'text-embedding-3-large'
        }
        {
          name: 'ServiceBus__FullyQualifiedNamespace'
          value: serviceBusEndpoint
        }
        {
          name: 'Queues__DocumentProcessingQueueName'
          value: 'document-processing-queue'
        }
      ]
      cors: {
        allowedOrigins: [
          'https://*.azurestaticapps.net'
          'http://localhost:5173'
          'http://localhost:3000'
        ]
        supportCredentials: true
      }
    }
  }
}

output name string = webApp.name
output principalId string = webApp.identity.principalId
output defaultHostName string = webApp.properties.defaultHostName
