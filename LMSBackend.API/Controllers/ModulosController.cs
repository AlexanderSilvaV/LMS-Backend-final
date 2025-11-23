using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModulosController : ControllerBase
    {
        private readonly ModuloService _moduloService;

        public ModulosController(ModuloService moduloService)
        {
            _moduloService = moduloService;
        }
        
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        [HttpGet("curso/{cursoId}")]
        public async Task<IActionResult> ObtenerPorCurso(int cursoId)
        {
            try
            {
                // Obtener información del usuario del token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                // Si es alumno, usar el método que verifica asignación
                if (userRole == "Alumno")
                {
                    var resultado = await _moduloService.ObtenerModulosPorCursoIdParaUsuarioAsync(cursoId);
                    if (!resultado.OperacionExitosa)
                        return StatusCode(resultado.Codigo, resultado.Mensaje);
                    return Ok(resultado.Dato);
                }

                // Para administradores y docentes, usar el método original
                var resultadoGeneral = await _moduloService.ObtenerModulosPorCursoIdAsync(cursoId);
                if (!resultadoGeneral.OperacionExitosa)
                    return StatusCode(resultadoGeneral.Codigo, resultadoGeneral.Mensaje);
                return Ok(resultadoGeneral.Dato);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
        
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ModuloCreacionDTO dto)
        {
            var resultado = await _moduloService.CrearModuloAsync(dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }
        
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPut("{moduloId}")]
        public async Task<IActionResult> Editar(int moduloId, [FromBody] ModuloEdicionDTO dto)
        {
            var resultado = await _moduloService.EditarModuloAsync(moduloId, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }
        
        [Authorize(Roles = "Administrador, Docente")]
        [HttpDelete("{moduloId}")]
        public async Task<IActionResult> Eliminar(int moduloId)
        {
            var resultado = await _moduloService.EliminarModuloAsync(moduloId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Mensaje);
        }
        
        [Authorize(Roles = "Docente, Alumno")]
        [HttpGet("por-curso/{cursoId}")]
        public async Task<IActionResult> ObtenerModulosPorCurso(int cursoId)
        {
            var resultado = await _moduloService.ObtenerModulosPorCursoIdParaUsuarioAsync(cursoId);

            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }
    }
}
