var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles(); // ← agregar
app.UseAuthorization();
app.MapControllers();
app.MapHub<ClientesHub>("/hubs/clientes");
app.Run();
