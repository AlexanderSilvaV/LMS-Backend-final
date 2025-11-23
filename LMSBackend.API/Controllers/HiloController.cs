using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HilosController : ControllerBase
    {
        private readonly HiloService _hiloService;

        public HilosController(HiloService hiloService)
        {
            _hiloService = hiloService;
        }

        // Crear hilo (alumno permitido si el foro lo permite)
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] HiloCreacionDTO dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _hiloService.CrearHiloAsync(dto.ForoId, dto.Titulo, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Fijar hilo (solo docente/admin)
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPatch("{hiloId:int}/pin")]
        public async Task<IActionResult> Fijar(int hiloId, [FromBody] HiloPinDTO dto)
        {
            var resultado = await _hiloService.FijarHiloAsync(hiloId, dto.PinnedOrder);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Quitar fijado (solo docente/admin)
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPatch("{hiloId:int}/unpin")]
        public async Task<IActionResult> QuitarFijado(int hiloId)
        {
            var resultado = await _hiloService.QuitarFijadoAsync(hiloId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Cerrar/Reabrir hilo (solo docente/admin)
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPatch("{hiloId:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int hiloId, [FromBody] HiloCerrarDTO dto)
        {
            var resultado = await _hiloService.CerrarHiloAsync(hiloId, dto.Cerrado);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Listar hilos del foro con filtros/paginación (miembro del curso o admin)
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        [HttpPost("{foroId:int}/listar")]
        public async Task<IActionResult> Listar(int foroId, [FromBody] HiloListadoDTO filtros)
        {
            var resultado = await _hiloService.ListarHilosAsync(foroId, filtros);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }
    }
}
