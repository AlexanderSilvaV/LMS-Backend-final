using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador, Docente, Alumno")]
    public class SuscripcionesController : ControllerBase
    {
        private readonly SuscripcionService _suscripcionService;

        public SuscripcionesController(SuscripcionService suscripcionService)
        {
            _suscripcionService = suscripcionService;
        }

        // POST: api/suscripciones/{hiloId}
        [HttpPost("{hiloId:int}")]
        public async Task<IActionResult> Suscribirse(int hiloId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _suscripcionService.SuscribirseAsync(hiloId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Mensaje);
        }

        // DELETE: api/suscripciones/{hiloId}
        [HttpDelete("{hiloId:int}")]
        public async Task<IActionResult> Desuscribirse(int hiloId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _suscripcionService.DesuscribirseAsync(hiloId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Mensaje);
        }

        // GET: api/suscripciones/mias?page=1&size=20
        [HttpGet("mias")]
        public async Task<IActionResult> MisSuscripciones([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _suscripcionService.MisSuscripcionesAsync(userId, page, size);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }
    }
}
