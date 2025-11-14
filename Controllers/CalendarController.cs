using Microsoft.AspNetCore.Mvc;
using RoomKioskAPI.Services;
using Microsoft.Graph.Models;

namespace RoomKioskAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CalendarController : ControllerBase
	{
		private readonly GraphService _graphService;

		public CalendarController(GraphService graphService)
		{
			_graphService = graphService;
		}

        //Obtener eventos de una sala (por tenant y correo)
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("✅ API funcionando correctamente");
        }

        [HttpGet("{tenant}/{roomEmail}")]
		public async Task<IActionResult> GetRoomEvents(string tenant, string roomEmail)
		{
			try
			{
				var events = await _graphService.GetRoomEventsAsync(tenant, roomEmail);

				if (events == null || !events.Any())
					return NotFound(new { message = "No hay eventos en este calendario." });

				return Ok(events);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}

		//Crear un nuevo evento en una sala
		[HttpPost("{tenant}/{roomEmail}")]
		public async Task<IActionResult> CreateEvent(
			string tenant,
			string roomEmail,
			[FromBody] CreateEventRequest request)
		{
			try
			{
				if (request == null || string.IsNullOrEmpty(request.Subject))
					return BadRequest(new { message = "Faltan datos del evento." });

				var createdEvent = await _graphService.CreateRoomEventAsync(
					tenant,
					roomEmail,
					request.Subject,
					request.Start,
					request.End
				);

				return Created("", new {success = true});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}
	}

	//Modelo de request para crear eventos
	public class CreateEventRequest
	{
		public string Subject { get; set; } = string.Empty;
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
}


