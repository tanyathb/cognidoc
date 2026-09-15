// Load configuration from the single source of truth
var config = loadJsonContent('../config.json')

var location = config.location
var serviceBusNamespaceName = config.serviceBusNamespaceName

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

// Queue for point-to-point document processing tasks
resource documentQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'document-processing-queue'
  properties: {
    enablePartitioning: false
    maxDeliveryCount: 10
  }
}

// Topic for publish/subscribe event fan-out
resource documentTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBus
  name: 'document-events'
  properties: {
    supportOrdering: true
  }
}

resource documentSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: documentTopic
  name: 'processor-sub'
  properties: {
    maxDeliveryCount: 10
  }
}

output serviceBusNamespaceId string = serviceBus.id
output name string = serviceBus.name
output serviceBusEndpoint string = '${serviceBus.name}.servicebus.windows.net'
