namespace CogniDoc.Functions.Extensions.Services;

public interface IRagSearchService
{
    Task EnsureIndexCreatedAsync(CancellationToken cancellationToken = default);
    Task IngestDocumentChunksAsync(string documentId, string fileName, List<string> textChunks, CancellationToken cancellationToken = default);
    Task<List<string>> SearchRelevantChunksAsync(string userQuery, int maxResults = 3, CancellationToken cancellationToken = default);
}