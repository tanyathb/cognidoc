namespace CogniDoc.Functions.Configuration;

public class ServiceBusOptions
{
    public const string SectionName = "Queues";

    // Configuration & Trigger Keys
    public const string ConnectionKey = "ServiceBusConnection";
    public const string NamespaceKey = "ServiceBus:FullyQualifiedNamespace";
    public const string DocumentProcessingQueueTrigger = "%Queues:DocumentProcessingQueueName%";

    // Bound properties
    public string DocumentProcessingQueueName { get; set; } = "document-processing-queue";
}