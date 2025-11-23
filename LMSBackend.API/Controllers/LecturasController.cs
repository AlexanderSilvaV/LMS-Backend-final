using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador, Docente, Alumno")]
    public class LecturasController : ControllerBase
    {
        private readonly LecturaService _lecturaService;

        public LecturasController(LecturaService lecturaService)
        {
            _lecturaService = lecturaService;
        }

        // POST: api/lecturas/{hiloId}/marcar
        [HttpPost("{hiloId:int}/marcar")]
        public async Task<IActionResult> MarcarComoLeido(int hiloId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _lecturaService.MarcarComoLeidoAsync(hiloId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato); // DateTime (LastReadAt)
        }

        // GET: api/lecturas/{hiloId}/unread-count
        [HttpGet("{hiloId:int}/unread-count")]
        public async Task<IActionResult> UnreadCount(int hiloId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _lecturaService.UnreadCountAsync(hiloId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato); // int
        }

        // POST: api/lecturas/{hiloId}/hasta/{postId}
        [HttpPost("{hiloId:int}/hasta/{postId:int}")]
        public async Task<IActionResult> MarcarHastaPost(int hiloId, int postId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _lecturaService.MarcarHastaPostAsync(hiloId, postId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato); // DateTime (LastReadAt actualizado)
        }
    }
}
