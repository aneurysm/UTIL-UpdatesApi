using Microsoft.AspNetCore.SignalR;

public class ClientesHub : Hub
{
    // Diccionario estático para rastrear clientes conectados
    // clave: ruc, valor: connectionId
    private static readonly Dictionary<string, ClienteConectado> _clientes = new();
    private static readonly object _lock = new();

    // La WebAPI del cliente se registra al conectarse
    public async Task RegistrarCliente(string ruc, string nombreEmpresa, string versionBd)
    {
        lock (_lock)
        {
            _clientes[ruc] = new ClienteConectado
            {
                Ruc = ruc,
                NombreEmpresa = nombreEmpresa,
                VersionBd = versionBd,
                ConnectionId = Context.ConnectionId,
                ConectadoEn = DateTime.Now
            };
        }

        Console.WriteLine($"[HUB] Cliente conectado: {nombreEmpresa} ({ruc}) v{versionBd}");

        // Notificar al panel que hay un nuevo cliente conectado
        await Clients.All.SendAsync("ClienteConectado", _clientes[ruc]);
    }

    // Devuelve la lista de clientes conectados al panel
    public Task<List<ClienteConectado>> ObtenerClientes()
    {
        lock (_lock)
        {
            return Task.FromResult(_clientes.Values.ToList());
        }
    }

    // El panel envía comando de migración a un cliente específico
    public async Task EnviarComandoMigrar(string ruc)
    {
        string? connectionId;
        lock (_lock)
        {
            connectionId = _clientes.ContainsKey(ruc) ? _clientes[ruc].ConnectionId : null;
        }

        if (connectionId == null)
        {
            await Clients.Caller.SendAsync("Error", $"Cliente {ruc} no está conectado");
            return;
        }

        Console.WriteLine($"[HUB] Enviando comando migrar a {ruc}");
        await Clients.Client(connectionId).SendAsync("EjecutarMigracion");
    }

    // La WebAPI del cliente reporta el resultado de la migración
    public async Task ReportarResultadoMigracion(string ruc, bool ok, string mensaje)
    {
        // Actualizar versión en el diccionario
        lock (_lock)
        {
            if (_clientes.ContainsKey(ruc))
                _clientes[ruc].UltimoResultado = ok ? $"OK: {mensaje}" : $"ERROR: {mensaje}";
        }

        Console.WriteLine($"[HUB] Resultado migración {ruc}: {(ok ? "OK" : "ERROR")} - {mensaje}");

        // Notificar al panel
        await Clients.All.SendAsync("ResultadoMigracion", new { ruc, ok, mensaje });
    }

    // Limpiar cuando un cliente se desconecta
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_lock)
        {
            var cliente = _clientes.Values.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);
            if (cliente != null)
            {
                _clientes.Remove(cliente.Ruc);
                Console.WriteLine($"[HUB] Cliente desconectado: {cliente.NombreEmpresa}");
            }
        }
        return base.OnDisconnectedAsync(exception);
    }
}

public class ClienteConectado
{
    public string Ruc { get; set; } = "";
    public string NombreEmpresa { get; set; } = "";
    public string VersionBd { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public DateTime ConectadoEn { get; set; }
    public string UltimoResultado { get; set; } = "";
}
