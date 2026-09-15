@description('Optional: Principal ID of the App Service Web API Managed Identity')
param apiPrincipalId string = ''

@description('Optional: Principal ID of the Azure Function App Managed Identity')
param functionPrincipalId string = ''

@description('Optional: Entra ID of the developer for local debugging')
param developerPrincipalId string = ''

param storageAccountName string
param searchServiceName string
param openAiResourceName string
param cosmosAccountName string
param serviceBusNamespaceName string

// ----------------------------------------------------------------------------
// Existing Resource Lookups
// ----------------------------------------------------------------------------
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = { name: storageAccountName }
resource search 'Microsoft.Search/searchServices@2024-03-01-preview' existing = { name: searchServiceName }
resource openAi 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' existing = { name: openAiResourceName }
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = { name: cosmosAccountName }
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = { name: serviceBusNamespaceName }

// ----------------------------------------------------------------------------
// Built-in Role Definitions
// ----------------------------------------------------------------------------
var roles = {
  // Storage
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageBlobDataContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  storageQueueDataContributor: '974c5e8b-45b9-4653-a493-b5813815108a'
  
  // AI & Search
  cognitiveServicesOpenAiUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
  searchIndexDataContributor: '8ebe5a00-7179-4914-b839-f5f30b7d44ae'
  
  // Messaging
  serviceBusDataOwner: '090c5cfd-751d-490a-894a-3ce6f1109419'
}

var cosmosSqlDataContributorId = '00000000-0000-0000-0000-000000000002'

// ============================================================================
// 1. FUNCTION APP MANAGED IDENTITY ASSIGNMENTS
// ============================================================================

// Azure Functions host requires Blob Data Owner for AzureWebJobsStorage
resource funcBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionPrincipalId)) {
  name: guid(storage.id, functionPrincipalId, roles.storageBlobDataOwner)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataOwner)
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Azure Functions host requires Queue Data Contributor for internal task queues/scaling
resource funcQueueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionPrincipalId)) {
  name: guid(storage.id, functionPrincipalId, roles.storageQueueDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageQueueDataContributor)
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource funcOpenAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionPrincipalId)) {
  name: guid(openAi.id, functionPrincipalId, roles.cognitiveServicesOpenAiUser)
  scope: openAi
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.cognitiveServicesOpenAiUser)
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource funcSearchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionPrincipalId)) {
  name: guid(search.id, functionPrincipalId, roles.searchIndexDataContributor)
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.searchIndexDataContributor)
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource funcServiceBusRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionPrincipalId)) {
  name: guid(serviceBus.id, functionPrincipalId, roles.serviceBusDataOwner)
  scope: serviceBus
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataOwner)
    principalId: functionPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource funcCosmosRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = if (!empty(functionPrincipalId)) {
  parent: cosmos
  name: guid(cosmos.id, functionPrincipalId, cosmosSqlDataContributorId)
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/${cosmosSqlDataContributorId}'
    principalId: functionPrincipalId
    scope: cosmos.id
  }
}

// ============================================================================
// 2. WEB API MANAGED IDENTITY ASSIGNMENTS
// ============================================================================

resource apiBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(storage.id, apiPrincipalId, roles.storageBlobDataContributor)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataContributor)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiOpenAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(openAi.id, apiPrincipalId, roles.cognitiveServicesOpenAiUser)
  scope: openAi
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.cognitiveServicesOpenAiUser)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiSearchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(search.id, apiPrincipalId, roles.searchIndexDataContributor)
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.searchIndexDataContributor)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource apiCosmosRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = if (!empty(apiPrincipalId)) {
  parent: cosmos
  name: guid(cosmos.id, apiPrincipalId, cosmosSqlDataContributorId)
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/${cosmosSqlDataContributorId}'
    principalId: apiPrincipalId
    scope: cosmos.id
  }
}

resource apiServiceBusRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiPrincipalId)) {
  name: guid(serviceBus.id, apiPrincipalId, roles.serviceBusDataOwner)
  scope: serviceBus
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataOwner)
    principalId: apiPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// 3. LOCAL DEVELOPER ASSIGNMENTS (Optional)
// ============================================================================

resource devBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(developerPrincipalId)) {
  name: guid(storage.id, developerPrincipalId, roles.storageBlobDataOwner)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataOwner)
    principalId: developerPrincipalId
    principalType: 'User'
  }
}

resource devOpenAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(developerPrincipalId)) {
  name: guid(openAi.id, developerPrincipalId, roles.cognitiveServicesOpenAiUser)
  scope: openAi
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.cognitiveServicesOpenAiUser)
    principalId: developerPrincipalId
    principalType: 'User'
  }
}

resource devSearchRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(developerPrincipalId)) {
  name: guid(search.id, developerPrincipalId, roles.searchIndexDataContributor)
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.searchIndexDataContributor)
    principalId: developerPrincipalId
    principalType: 'User'
  }
}

resource devServiceBusRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(developerPrincipalId)) {
  name: guid(serviceBus.id, developerPrincipalId, roles.serviceBusDataOwner)
  scope: serviceBus
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataOwner)
    principalId: developerPrincipalId
    principalType: 'User'
  }
}

resource devCosmosRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = if (!empty(developerPrincipalId)) {
  parent: cosmos
  name: guid(cosmos.id, developerPrincipalId, cosmosSqlDataContributorId)
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/${cosmosSqlDataContributorId}'
    principalId: developerPrincipalId
    scope: cosmos.id
  }
}
