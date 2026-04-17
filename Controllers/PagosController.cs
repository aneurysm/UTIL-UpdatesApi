using Microsoft.AspNetCore.Mvc;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly IConfiguration _config;

    public PagosController(IConfiguration config)
    {
        _config = config;
    }

    // GET api/pagos — todos los registros
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");
            var lista = new List<object>();

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT ruc, razonsocial, estadocobro, fechavalidlic, 
                       comentariocontacto, comentariocobranza
                FROM f_e_empresa_pse_cliente_validpago
                ORDER BY razonsocial", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    ruc                 = reader.GetString(0),
                    razonSocial         = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    estadoCobro         = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    fechaValidLic       = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("dd/MM/yyyy"),
                    comentarioContacto  = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    comentarioCobranza  = reader.IsDBNull(5) ? "" : reader.GetString(5)
                });
            }

            return Ok(lista);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET api/pagos/{ruc} — por RUC específico
    [HttpGet("{ruc}")]
    public async Task<IActionResult> GetByRuc(string ruc)
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT ruc, razonsocial, estadocobro, fechavalidlic,
                       comentariocontacto, comentariocobranza
                FROM f_e_empresa_pse_cliente_validpago
                WHERE ruc = @Ruc", conn);

            cmd.Parameters.AddWithValue("Ruc", ruc);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { mensaje = $"Cliente {ruc} no encontrado" });

            return Ok(new
            {
                ruc                = reader.GetString(0),
                razonSocial        = reader.IsDBNull(1) ? "" : reader.GetString(1),
                estadoCobro        = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                fechaValidLic      = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("dd/MM/yyyy"),
                comentarioContacto = reader.IsDBNull(4) ? "" : reader.GetString(4),
                comentarioCobranza = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET api/pagos/{ruc}/{codigoTienda}
    [HttpGet("{ruc}/{codigoTienda}")]
    public async Task<IActionResult> GetByRucYTienda(string ruc, string codigoTienda)
    {
        try
        {
            var cs = _config.GetConnectionString("DropletDB");

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT ruc, razonsocial, estadocobro, fechavalidlic,
                    comentariocontacto, comentariocobranza
                FROM f_e_empresa_pse_cliente_validpago
                WHERE ruc = @Ruc 
                AND codigo_tienda = @CodigoTienda", conn);

            cmd.Parameters.AddWithValue("Ruc", ruc);
            cmd.Parameters.AddWithValue("CodigoTienda", codigoTienda);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { mensaje = $"Cliente {ruc} con tienda {codigoTienda} no encontrado" });

            return Ok(new
            {
                ruc                = reader.GetString(0),
                razonSocial        = reader.IsDBNull(1) ? "" : reader.GetString(1),
                estadoCobro        = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                fechaValidLic      = reader.IsDBNull(3) ? null : reader.GetDateTime(3).ToString("dd/MM/yyyy"),
                comentarioContacto = reader.IsDBNull(4) ? "" : reader.GetString(4),
                comentarioCobranza = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}