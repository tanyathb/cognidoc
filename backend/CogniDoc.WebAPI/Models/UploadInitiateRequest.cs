namespace CogniDoc.WebAPI.Models;

public class UploadInitiateRequest
{
    public string Filename { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
