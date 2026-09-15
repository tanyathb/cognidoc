using System;
using System.Collections.Generic;
using System.Text;

namespace CogniDoc.Functions.Models
{
    public sealed class DocumentProcessingResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int CharacterCount { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
