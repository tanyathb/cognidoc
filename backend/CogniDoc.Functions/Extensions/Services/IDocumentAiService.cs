namespace CogniDoc.Functions.Extensions.Services;

public interface IDocumentAiService
{
    Task<string> GenerateSummaryAsync(string documentText, CancellationToken cancellationToken = default);
}