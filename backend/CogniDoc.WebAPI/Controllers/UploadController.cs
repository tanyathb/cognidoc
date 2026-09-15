using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CogniDoc.WebAPI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CogniDoc.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger<UploadController> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly StorageOptions _storageOptions;

    public UploadController(
        BlobServiceClient blobServiceClient,
        ILogger<UploadController> logger,
        IOptions<StorageOptions> storageOptions)
    {
        _blobServiceClient = blobServiceClient;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No document payload received." });
        }

        try
        {
            _logger.LogInformation("Processing upload for file: {FileName} ({Size} bytes)", file.FileName, file.Length);

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_storageOptions.ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            string uniqueBlobName = $"{Guid.NewGuid()}-{file.FileName}";
            BlobClient blobClient = containerClient.GetBlobClient(uniqueBlobName);

            await using (var stream = file.OpenReadStream())
            {
                var blobHttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };
                await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
            }

            _logger.LogInformation("File successfully uploaded to blob destination: {Uri}", blobClient.Uri);

            return Ok(new
            {
                success = true,
                blobName = uniqueBlobName,
                blobUrl = blobClient.Uri.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload processing failed for file: {FileName}", file.FileName);
            return StatusCode(500, new { success = false, message = $"Internal server storage failure: {ex.Message}" });
        }
    }
}