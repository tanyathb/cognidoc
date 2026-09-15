using System;
using System.Collections.Generic;
using System.Text;

namespace CogniDoc.Functions.Models
{
    public sealed class DocumentProcessingMessage
    {
        public string DocumentId { get; set; } = Guid.NewGuid().ToString();
        public string BlobUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
