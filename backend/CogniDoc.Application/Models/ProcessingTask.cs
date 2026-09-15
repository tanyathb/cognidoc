using System;
using System.Collections.Generic;
using System.Text;

namespace CogniDoc.Application.Models
{
    public class DocumentProcessingMessage
    {
        public required string JobId { get; init; }
        public required string FileUrl { get; init; }
        public required long FileSizeBytes { get; init; }
        public required string ContentType { get; init; }
        public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
    }
}
