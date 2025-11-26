using RoomKioskAPI.Services;
using Microsoft.OpenApi.Models;
using DotNetEnv; // 👈 Importante para leer el archivo .env

// ✅ Cargar variables desde archivo .env (si existe)
Env.Load();
var clientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");

Console.WriteLine($"🧩 Cargando credenciales desde .env...");
Console.WriteLine($"   ClientId: {clientId}");
Console.WriteLine($"   Secret: {clientSecret?.Substring(0, 5)}...");

// 🔧 Configuración del builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Habilitar CORS para permitir peticiones desde Expo o navegador local
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExpoApp", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin =>
                origin.Contains("localhost") ||
                origin.Contains("192.168.") ||
                origin.Contains("109.123.245.32") ||
                origin.Contains("reinventedschools.com") ||
                origin.Contains("room.reinventedschools.com")); // dominio de producción
    });
});

// ✅ Controladores
builder.Services.AddControllers();

// ✅ Swagger (documentación de la API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Room Kiosk API",
        Version = "v1",
        Description = "API para reservar salas de los colegios"
    });
});

// ✅ Servicio Graph
builder.Services.AddScoped<GraphService>();

var app = builder.Build();

// ✅ Swagger visible en todo entorno
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Room Kiosk API v1");
    c.RoutePrefix = string.Empty; // abre Swagger directamente
});

// ❌ HTTPS desactivado temporalmente para entorno local
// app.UseHttpsRedirection();

// ✅ CORS global
app.UseCors("AllowExpoApp");

app.MapControllers();

app.Run();
