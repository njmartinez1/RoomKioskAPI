using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoomKioskAPI.Services
{
    public class GraphService
    {
        private readonly IConfiguration _config;

        public GraphService(IConfiguration config)
        {
            _config = config;
        }

        // 🔐 Autenticación del cliente Graph
        private GraphServiceClient GetGraphClient(string tenantId)
        {
            // Leer SIEMPRE desde .env
            var clientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");

            Console.WriteLine("🔍 Credenciales usadas por GraphService:");
            Console.WriteLine($"   ClientId: {clientId}");
            Console.WriteLine($"   Secret: {clientSecret?.Substring(0, 5)}...");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("❌ Las credenciales GRAPH_CLIENT_ID o GRAPH_CLIENT_SECRET no fueron cargadas.");
            }
            var credential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret
            );

            return new GraphServiceClient(credential);
        }

        // 📅 Obtener eventos de una sala
        public async Task<IEnumerable<object>> GetRoomEventsAsync(string tenantKey, string roomEmail)
        {
            Console.WriteLine($"📡 [GraphService] GetRoomEventsAsync iniciado - Tenant: {tenantKey}, Sala: {roomEmail}");

            var tenantId = _config[$"MicrosoftGraph:Tenants:{tenantKey}"];
            if (string.IsNullOrEmpty(tenantId))
            {
                Console.WriteLine("❌ Tenant ID no encontrado, revisa appsettings.json");
                return new List<object>();
            }

            var graphClient = GetGraphClient(tenantId);
            Console.WriteLine("🔑 GraphClient creado correctamente.");

            // Rango del día actual (hora UTC)
            var today = DateTime.UtcNow.Date;
            var start = today.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var end = today.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            try
            {
                Console.WriteLine($"📆 Solicitando eventos entre {start} y {end}");

                // ✅ Incluye zona horaria local (Ecuador)
                var eventsResponse = await graphClient.Users[roomEmail].CalendarView
                    .GetAsync(opt =>
                    {
                        opt.QueryParameters.StartDateTime = start;
                        opt.QueryParameters.EndDateTime = end;
                        opt.Headers.Add("Prefer", "outlook.timezone=\"America/Guayaquil\"");
                    });

                if (eventsResponse?.Value == null || !eventsResponse.Value.Any())
                {
                    Console.WriteLine("⚠️ No se encontraron eventos para este rango.");
                    return new List<object>();
                }

                Console.WriteLine($"✅ Se encontraron {eventsResponse.Value.Count} eventos.");

                // 🧩 Normalizamos la salida para el frontend React
                var result = eventsResponse.Value.Select(ev => new
                {
                    id = ev.Id,
                    subject = ev.Subject,
                    start = ev.Start,
                    end = ev.End,
                    organizer = ev.Organizer,
                    attendees = ev.Attendees
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en GraphService: {ex.Message}");
                return new List<object>();
            }
        }

        // 📝 Crear un nuevo evento en la sala
        public async Task<Event?> CreateRoomEventAsync(
            string tenantKey,
            string roomEmail,
            string subject,
            DateTime start,
            DateTime end)
        {
            var tenantId = _config[$"MicrosoftGraph:Tenants:{tenantKey}"];
            if (string.IsNullOrEmpty(tenantId))
            {
                Console.WriteLine("❌ Tenant ID no encontrado al crear evento.");
                return null;
            }

            var graphClient = GetGraphClient(tenantId);

            var newEvent = new Event
            {
                Subject = subject,
                Organizer = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = roomEmail,               // 👈 La sala es el organizador
                    }
                },
                Start = new DateTimeTimeZone
                {
                    DateTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "America/Guayaquil"
                },
                End = new DateTimeTimeZone
                {
                    DateTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "America/Guayaquil"
                },
                Location = new Location
                {
                    DisplayName = roomEmail
                    
                }
            };

            try
            {
                Console.WriteLine($"🆕 Creando evento: {subject} en {roomEmail}");
                var created = await graphClient.Users[roomEmail].Events.PostAsync(newEvent);
                Console.WriteLine("✅ Evento creado correctamente en Microsoft Graph.");
                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error al crear evento:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    Console.WriteLine("🔍 InnerException:");
                    Console.WriteLine(ex.InnerException.Message);
                }

                return null;
            }

        }
    }
}
