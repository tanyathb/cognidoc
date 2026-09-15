namespace CogniDoc.WebAPI.Models;

public class UploadInitiateResponse
{
    public string SasUrl { get; set; } = string.Empty;
    public string TrackingId { get; set; } = string.Empty;
}
