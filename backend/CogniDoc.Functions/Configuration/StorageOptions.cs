using System;
using System.Collections.Generic;
using System.Text;

namespace CogniDoc.Functions.Configuration
{
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        // Attribute binding constants for BlobTrigger
        public const string ConnectionKey = "Storage:BlobConnection";
        public const string EndpointKey = "Storage:BlobEndpoint";
        public const string ContainerTrigger = "%Storage:ContainerName%/{name}";

        // Bound properties for runtime DI
        public string ContainerName { get; set; } = "documents";
        public string BlobEndpoint { get; set; } = string.Empty;
        public string BlobConnection { get; set; } = string.Empty;
    }
}
