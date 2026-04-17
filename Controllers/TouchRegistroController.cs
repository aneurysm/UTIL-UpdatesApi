using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace UpdatesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TouchRegistroController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TouchRegistroController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("registrar")]
	public async Task<IActionResult> Registrar([FromBody] TouchRegistroDto dto)
	{
    		Console.WriteLine($"[DEBUG] Inicio - RUC: {dto.Ruc}");

    if (string.IsNullOrWhiteSpace(dto.Ruc) || string.IsNullOrWhiteSpace(dto.Version))
        return BadRequest("RUC y Version son requeridos.");

    try
    {
        var cs = _config.GetConnectionString("DropletDB");
        Console.WriteLine($"[DEBUG] ConnectionString: {cs}");

        await using var conn = new NpgsqlConnection(cs);
        Console.WriteLine("[DEBUG] Intentando abrir conexión...");
        await conn.OpenAsync();
        Console.WriteLine("[DEBUG] Conexión abierta OK");

        var sqlCliente = @"
            INSERT INTO clientes (ruc, razon_social, codigo_tienda, actualizado_en)
            VALUES (@Ruc, @RazonSocial, @CodigoTienda, NOW())
            ON CONFLICT (ruc) DO UPDATE
                SET razon_social   = EXCLUDED.razon_social,
                    codigo_tienda  = EXCLUDED.codigo_tienda,
                    actualizado_en = NOW()
            RETURNING id;";

        await using var cmdCliente = new NpgsqlCommand(sqlCliente, conn);
        cmdCliente.Parameters.AddWithValue("Ruc",          dto.Ruc);
        cmdCliente.Parameters.AddWithValue("RazonSocial",  dto.NombreEmpresa  ?? "");
        cmdCliente.Parameters.AddWithValue("CodigoTienda", dto.CodigoTienda    ?? "");
        Console.WriteLine("[DEBUG] Ejecutando INSERT clientes...");
        var clienteId = (int)(await cmdCliente.ExecuteScalarAsync())!;
        Console.WriteLine($"[DEBUG] Cliente ID: {clienteId}");

        var sqlTouch = @"
            INSERT INTO touch_registros
                (cliente_id, nombre_equipo, nro_ptoventa, version, version_bd, ultimo_evento, ip_local, fecha_evento, tipo, plataforma)
            VALUES
                (@ClienteId, @Maquina, @PtoVenta, @Version, @VersionBd, @Evento, @Ip, NOW(), @Tipo, @Plataforma);";

        await using var cmdTouch = new NpgsqlCommand(sqlTouch, conn);
        cmdTouch.Parameters.AddWithValue("ClienteId",  clienteId);
        cmdTouch.Parameters.AddWithValue("Maquina",    dto.NombreMaquina ?? "");
        cmdTouch.Parameters.AddWithValue("PtoVenta",   dto.NroPtoVenta   ?? "");
        cmdTouch.Parameters.AddWithValue("Version",    dto.Version);
	cmdTouch.Parameters.AddWithValue("VersionBd",  dto.VersionBd ?? "");
        cmdTouch.Parameters.AddWithValue("Evento",     dto.Evento);
        cmdTouch.Parameters.AddWithValue("Ip",         dto.IpLocal       ?? "");
	cmdTouch.Parameters.AddWithValue("Tipo",       dto.Tipo ?? "touch");
	cmdTouch.Parameters.AddWithValue("Plataforma", dto.Plataforma ?? "windows");

        Console.WriteLine("[DEBUG] Ejecutando INSERT touch_registros...");
        await cmdTouch.ExecuteNonQueryAsync();
        Console.WriteLine("[DEBUG] Todo OK");

        return Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DEBUG] EXCEPCION: {ex.Message}");
        return StatusCode(500, new { error = ex.Message });
    }
}
 
    }

    public class TouchRegistroDto
    {
        public string? Ruc           { get; set; }
        public string? NombreEmpresa { get; set; }
        public string? NombreMaquina { get; set; }
        public string? NroPtoVenta   { get; set; }
        public string? Version       { get; set; }
        public string? Evento        { get; set; }
        public string? IpLocal       { get; set; }
	public string? CodigoTienda  { get; set; }
	public string? VersionBd     { get; set; }
	public string? Tipo          { get; set; }  // ← nuevo
    	public string? Plataforma    { get; set; }  // ← nuevo
    }
}
