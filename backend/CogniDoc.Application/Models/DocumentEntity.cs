using System.Text.Json.Serialization; // Or using Newtonsoft.Json;

public class DocumentMetadataEntity // or your entity class name
{
    [JsonPropertyName("id")] // [JsonProperty("id")] for Newtonsoft
    public string Id { get; set; } = default!;

    [JsonPropertyName("partitionKey")] // [JsonProperty("partitionKey")] for Newtonsoft
    public string PartitionKey { get; set; } = default!;

    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = default!;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = default!;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = default!;

    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; set; }

    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; }
}