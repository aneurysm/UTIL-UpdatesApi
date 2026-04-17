using Microsoft.AspNetCore.Mvc;
using UpdatesApi.Models;

namespace UpdatesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdatesController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UpdatesController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("check")]
        public IActionResult Check([FromQuery] string appName, [FromQuery] string currentVersion)
        {
            // Por ahora la versión está en appsettings.json
            // Luego lo conectamos a PostgreSQL
            var latest = _config.GetSection($"Apps:{appName}").Get<AppVersion>();

            if (latest == null)
                return NotFound(new { message = "Aplicación no encontrada" });

            var hasUpdate = new Version(latest.Version) > new Version(currentVersion);

            return Ok(new
            {
                hasUpdate,
                latestVersion = latest.Version,
                downloadUrl = hasUpdate ? latest.DownloadUrl : null,
                checksum = hasUpdate ? latest.Checksum : null,
                releaseNotes = hasUpdate ? latest.ReleaseNotes : null
            });
        }
    }
}
