targetScope = 'resourceGroup'

@description('Optional: Entra Object ID of the developer for local RBAC debugging')
param developerPrincipalId string = ''

module storage 'modules/storage.bicep' = {
  name: 'storageDeploy'
}

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmosDeploy'
}

module search 'modules/search.bicep' = {
  name: 'searchDeploy'
}

module openAi 'modules/openai.bicep' = {
  name: 'openAiDeploy'
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'serviceBusDeploy'
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticWebAppDeploy'
}

module appService 'modules/appservice.bicep' = {
  name: 'appServiceDeploy'
  params: {
    blobEndpoint: storage.outputs.blobEndpoint
    cosmosEndpoint: cosmos.outputs.documentDbEndpoint
    searchEndpoint: search.outputs.searchEndpoint
    openAiEndpoint: openAi.outputs.openAiEndpoint
    serviceBusEndpoint: serviceBus.outputs.serviceBusEndpoint
  }
}

module functionApp 'modules/functionapp.bicep' = {
  name: 'functionAppDeploy'
  params: {
    storageAccountName: storage.outputs.name
    blobEndpoint: storage.outputs.blobEndpoint
    cosmosEndpoint: cosmos.outputs.documentDbEndpoint
    searchEndpoint: search.outputs.searchEndpoint
    openAiEndpoint: openAi.outputs.openAiEndpoint
    serviceBusNamespaceName: serviceBus.outputs.name
  }
}

module rbac 'modules/rbac.bicep' = {
  name: 'rbacDeploy'
  params: {
    apiPrincipalId: appService.outputs.principalId
    functionPrincipalId: functionApp.outputs.principalId
    developerPrincipalId: developerPrincipalId
    storageAccountName: storage.outputs.name
    cosmosAccountName: cosmos.outputs.name
    searchServiceName: search.outputs.name
    openAiResourceName: openAi.outputs.name
    serviceBusNamespaceName: serviceBus.outputs.name
  }
}

output BLOB_ENDPOINT string = storage.outputs.blobEndpoint
output COSMOS_ENDPOINT string = cosmos.outputs.documentDbEndpoint
output SEARCH_ENDPOINT string = search.outputs.searchEndpoint
output OPENAI_ENDPOINT string = openAi.outputs.openAiEndpoint
output SERVICEBUS_ENDPOINT string = serviceBus.outputs.serviceBusEndpoint
output API_HOSTNAME string = appService.outputs.defaultHostName
output SWA_HOSTNAME string = staticWebApp.outputs.defaultHostname
output FUNCTION_HOSTNAME string = functionApp.outputs.defaultHostName
