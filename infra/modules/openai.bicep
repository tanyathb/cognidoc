// Load configuration from the single source of truth
var config = loadJsonContent('../config.json')

var location = config.location
var openAiResourceName = config.openAiResourceName

resource openAi 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: openAiResourceName
  location: location
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: openAiResourceName
    publicNetworkAccess: 'Enabled'
  }
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAi
  name: 'text-embedding-3-large'
  properties: {
    model: { format: 'OpenAI', name: 'text-embedding-3-large', version: '1' }
  }
  sku: { name: 'Standard', capacity: 1 } // Lowered from 10 to 1 (1,000 TPM)
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAi
  name: 'gpt-4o'
  dependsOn: [ embeddingDeployment ]
  properties: {
    model: { 
      format: 'OpenAI'
      name: 'gpt-4o'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
  sku: { name: 'GlobalStandard', capacity: 1 } // Lowered from 10 to 1 (1,000 TPM)
}

output openAiEndpoint string = openAi.properties.endpoint
output name string = openAi.name
