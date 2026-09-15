// Load configuration from the single source of truth
var config = loadJsonContent('../config.json')

var location = config.location
var cosmosAccountName = config.cosmosAccountName
var databaseName = 'CogniDocDb'
var containerName = 'Documents'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [{ locationName: location, failoverPriority: 0 }]
    capabilities: [{ name: 'EnableServerless' }] // Cost savings: pay only per request
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: { id: databaseName }
  }
}

resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: database
  name: containerName
  properties: {
    resource: {
      id: containerName
      partitionKey: { paths: ['/partitionKey'], kind: 'Hash' }
    }
  }
}

output cosmosAccountId string = cosmosAccount.id
output name string = cosmosAccount.name
output documentDbEndpoint string = cosmosAccount.properties.documentEndpoint
