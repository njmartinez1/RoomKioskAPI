using RoomKioskAPI.Services;
using Microsoft.OpenApi.Models;

var clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");
var clientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");

var builder = WebApplication.CreateBuilder(args);

// ✅ Habilitar CORS para permitir peticiones desde Expo
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
                origin.Contains("192.168.") // permite dispositivos en tu LAN
                //"http://room.reinventedpuembo.edu.ec" // dominio de producción (ajusta si cambia)
            );
    });
});


// ✅ Habilita controladores
builder.Services.AddControllers();

// ✅ Habilita Swagger (documentación de la API)
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

// ✅ Inyecta tu servicio GraphService
builder.Services.AddScoped<GraphService>();

var app = builder.Build();

// ✅ Middleware de Swagger (activado siempre)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Room Kiosk API v1");
    c.RoutePrefix = string.Empty; // abre Swagger directamente en http://localhost:5130/
});

// ❌ Desactiva redirección HTTPS si te daba error
// app.UseHttpsRedirection();


// ✅ Aplica la política CORS globalmente
app.UseCors("AllowExpoApp");

app.MapControllers();
app.Run();
