using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
public class PanelController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHubContext<ClientesHub> _hub;

    public PanelController(IConfiguration config, IHubContext<ClientesHub> hub)
    {
        _config = config;
        _hub = hub;
    }

    // GET api/panel/clientes — lista todos los clientes de PostgreSQL
    [HttpGet("clientes")]
    public async Task<IActionResult> GetClientes()
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");
            var lista = new List<object>();

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT id, ruc, razon_social, codigo_tienda, 
                       grupo, es_beta, activo, notas, actualizado_en
                FROM clientes 
                WHERE activo = true
                ORDER BY razon_social", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    id           = reader.GetInt32(0),
                    ruc          = reader.GetString(1),
                    razonSocial  = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    codigoTienda = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    grupo        = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    esBeta       = reader.IsDBNull(5) ? false : reader.GetBoolean(5),
                    activo       = reader.GetBoolean(6),
                    notas        = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    actualizadoEn = reader.IsDBNull(8) ? "" : reader.GetDateTime(8).ToString("dd/MM/yyyy HH:mm")
                });
            }

            return Ok(lista);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // PUT api/panel/clientes/{ruc}/grupo — asignar grupo a un cliente
    [HttpPut("clientes/{ruc}/grupo")]
    public async Task<IActionResult> AsignarGrupo(string ruc, [FromBody] AsignarGrupoDto dto)
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE clientes 
                SET grupo = @Grupo, es_beta = @EsBeta, notas = @Notas
                WHERE ruc = @Ruc", conn);

            cmd.Parameters.AddWithValue("Ruc",    ruc);
            cmd.Parameters.AddWithValue("Grupo",  dto.Grupo ?? "produccion");
            cmd.Parameters.AddWithValue("EsBeta", dto.EsBeta);
            cmd.Parameters.AddWithValue("Notas",  dto.Notas ?? "");

            await cmd.ExecuteNonQueryAsync();
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST api/panel/migrar/grupo/{grupo} — migrar todos los clientes de un grupo
    [HttpPost("migrar/grupo/{grupo}")]
    public async Task<IActionResult> MigrarGrupo(string grupo)
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                "SELECT ruc FROM clientes WHERE grupo = @Grupo AND activo = true", conn);
            cmd.Parameters.AddWithValue("Grupo", grupo);

            var rucs = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                rucs.Add(reader.GetString(0));

            // Enviar comando a cada cliente conectado del grupo
            foreach (var ruc in rucs)
                await _hub.Clients.All.SendAsync("EjecutarMigracionSiEsCliente", ruc);

            return Ok(new { ok = true, clientesNotificados = rucs.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST api/panel/migrar/todos — migrar todos los clientes
    [HttpPost("migrar/todos")]
    public async Task<IActionResult> MigrarTodos()
    {
        try
        {
            await _hub.Clients.All.SendAsync("EjecutarMigracion");
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    // GET api/panel/terminales
    [HttpGet("terminales")]
    public async Task<IActionResult> GetTerminales()
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");
            var lista = new List<object>();

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            // Trae el último registro por cliente+equipo
            await using var cmd = new NpgsqlCommand(@"
                SELECT DISTINCT ON (tr.cliente_id, tr.nombre_equipo)
                    c.ruc,
                    tr.nombre_equipo,
                    tr.nro_ptoventa,
                    tr.tipo,
                    tr.plataforma,
                    tr.version,
                    tr.version_bd,
                    tr.ip_local,
                    tr.fecha_evento
                FROM touch_registros tr
                JOIN clientes c ON c.id = tr.cliente_id
                ORDER BY tr.cliente_id, tr.nombre_equipo, tr.fecha_evento DESC", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    ruc          = reader.GetString(0),
                    nombreEquipo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    nroPtoVenta  = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    tipo         = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    plataforma   = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    version      = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    versionBd    = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    ipLocal      = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    fechaEvento  = reader.IsDBNull(8) ? "" 
                                : reader.GetDateTime(8).ToString("dd/MM/yyyy HH:mm")
                });
            }

            return Ok(lista);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class AsignarGrupoDto
{
    public string? Grupo  { get; set; }
    public bool    EsBeta { get; set; }
    public string? Notas  { get; set; }
}
