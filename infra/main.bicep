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

module rbac 'modules/rbac.bicep' = {
  name: 'rbacDeploy'
  params: {
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
