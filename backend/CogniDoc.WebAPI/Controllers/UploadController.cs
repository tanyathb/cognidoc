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

            //string connectionString = configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
 
            //_blobServiceClient = new BlobServiceClient(connectionString);
        }

        //[HttpPost("initiate")]
        //public async Task<ActionResult<UploadInitiateResponse>> InitiateUpload([FromBody] UploadInitiateRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.Filename))
        //    {
        //        return BadRequest("Invalid payload: Filename is required.");
        //    }

        //    try
        //    {
        //        _logger.LogInformation("Initiating upload handshake for: {Filename} ({Size} bytes)", request.Filename, request.FileSizeBytes);

        //        // 1. Ensure the 'uploads' container exists in the storage account
        //        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        //        await containerClient.CreateIfNotExistsAsync();


        //        // 2. Create a unique path inside the container to prevent filename collisions
        //        string uniqueId = Guid.NewGuid().ToString();
        //        string uniqueBlobName = $"{uniqueId}-{request.Filename}";
        //        BlobClient blobClient = containerClient.GetBlobClient(uniqueBlobName);


        //        // 3. Define the security clearance parameters for the SAS Token
        //        var sasBuilder = new BlobSasBuilder
        //        {
        //            BlobContainerName = ContainerName,
        //            BlobName = uniqueBlobName,
        //            Resource = "b", // "b" indicates a Blob resource access privilege
        //            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
        //        };

        //        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);


        //        // 4. Generate the signed URI string
        //        Uri secureUri = blobClient.GenerateSasUri(sasBuilder);

        //        _logger.LogInformation("Secure SAS token generated successfully for Tracking ID: {TrackingId}", uniqueId);


        //        // 5. Hand the structural payload tokens back to the React UI node
        //        return Ok(new UploadInitiateResponse
        //        {
        //            SasUrl = secureUri.ToString(),
        //            TrackingId = uniqueId
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to provision secure cloud SAS ticket for file: {Filename}", request.Filename);
        //        return StatusCode(500, "Internal storage subsystem error processing upload handshake.");
        //    }
        //}


        [HttpPost("local-direct-upload")]
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
