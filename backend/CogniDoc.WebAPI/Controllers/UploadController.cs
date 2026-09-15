using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CogniDoc.WebAPI.Configuration;
using CogniDoc.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CogniDoc.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : Controller
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

       
        [HttpPost("upload")]
        [DisableRequestSizeLimit] // Allows heavy enterprise document uploads without IIS blocking
        public async Task<IActionResult> LocalDirectUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No document payload received.");
            }

            try
            {
                _logger.LogInformation("Proxied server-side upload running for: {Name}", file.FileName);

                // 1. Target the local container destination
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_storageOptions.ContainerName);
                await containerClient.CreateIfNotExistsAsync();

                // 2. Format a clean unique name matching our original tracking architecture
                string uniqueBlobName = $"{Guid.NewGuid()}-{file.FileName}";
                BlobClient blobClient = containerClient.GetBlobClient(uniqueBlobName);

                // 3. Stream the file directly into Azurite server-side 
                using (var stream = file.OpenReadStream())
                {
                    var blobHttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };
                    await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
                }

                _logger.LogInformation("File successfully written to Azurite storage node: {Url}", blobClient.Uri);

                // Return a success payload to the React frontend component node
                return Ok(new { success = true, blobUrl = blobClient.Uri.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server-side proxy injection workflow failed.");
                return StatusCode(500, $"Internal server storage malfunction: {ex.Message}");
            }
        }

    }
}
