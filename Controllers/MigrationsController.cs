using Microsoft.AspNetCore.Mvc;

namespace UpdatesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public MigrationsController(IConfiguration config)
        {
            _config = config;
        }

        // GET api/migrations/pending?versionActual=2.9.0
        [HttpGet("pending")]
        public IActionResult GetPending([FromQuery] string versionActual)
        {
            if (string.IsNullOrWhiteSpace(versionActual))
                return BadRequest("versionActual es requerido.");

            var migrations = _config
                .GetSection("Migrations")
                .Get<List<MigrationInfo>>();

            if (migrations == null || migrations.Count == 0)
                return Ok(new List<MigrationInfo>());

            // Devuelve solo los scripts con versión mayor a la actual
            var pendientes = migrations
                .Where(m => new Version(m.Version) > new Version(versionActual))
                .OrderBy(m => new Version(m.Version))
                .ToList();

            return Ok(pendientes);
        }
    }

    public class MigrationInfo
    {
        public string Version    { get; set; } = "";
        public string Url        { get; set; } = "";
        public string Descripcion { get; set; } = "";
    }
}
