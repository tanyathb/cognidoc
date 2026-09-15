// Load configuration from the single source of truth
var config = loadJsonContent('../config.json')

var location = config.location
var searchServiceName = config.searchServiceName

resource search 'Microsoft.Search/searchServices@2024-03-01-preview' = {
  name: searchServiceName
  location: location
  sku: { name: 'basic' } // Lowest cost tier supporting vector search
  properties: {
    replicaCount: 1
    partitionCount: 1
    authOptions: { aadOrApiKey: { aadAuthFailureMode: 'http401WithBearerChallenge' } }
  }
}

output searchServiceId string = search.id
output name string = search.name
output searchEndpoint string = 'https://${searchServiceName}.search.windows.net'
