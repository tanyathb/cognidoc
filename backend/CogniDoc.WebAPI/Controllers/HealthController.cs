using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CogniDoc.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public HealthController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Basic health check probe endpoint
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "CogniDoc.WebAPI",
                environment = _environment.EnvironmentName,
                timestampUtc = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Optional ping route returning plain text
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }
    }
}