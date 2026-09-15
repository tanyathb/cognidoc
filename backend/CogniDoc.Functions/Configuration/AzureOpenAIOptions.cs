using System.ComponentModel.DataAnnotations;

namespace CogniDoc.Functions.Configuration;

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Local dev API key to bypass Managed Identity / IMDS probes.
    /// Omitted in Azure environments to trigger DefaultAzureCredential.
    /// </summary>
    public string? ApiKey { get; set; }

    public string ChatDeploymentName { get; set; } = "gpt-5-mini";

    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-large";
}