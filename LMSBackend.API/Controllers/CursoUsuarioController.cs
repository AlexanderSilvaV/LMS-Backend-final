using LMSBackend.API.Helpers;
using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/curso-usuarios")]
    [Authorize]
    public class CursoUsuarioController : ControllerBase
    {
        private readonly CursoUsuarioService _cursoUsuarioService;

        public CursoUsuarioController(CursoUsuarioService cursoUsuarioService)
        {
            _cursoUsuarioService = cursoUsuarioService;
        }

        [HttpPost("asignar")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ResultadoOperacion<CursoUsuarioDTO>>> AsignarUsuarioACurso([FromBody] AsignacionUsuarioCursoDTO dto)
        {
            var resultado = await _cursoUsuarioService.AsignarUsuarioACursoAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpDelete("{cursoId}/{usuarioId}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ResultadoOperacion<string>>> EliminarAsignacion(int cursoId, string usuarioId)
        {
            var resultado = await _cursoUsuarioService.EliminarAsignacionAsync(cursoId, usuarioId);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("curso/{cursoId}")]
        [Authorize(Roles = "Administrador,Docente,Profesor")]
        public async Task<ActionResult<ResultadoOperacion<List<CursoUsuarioDTO>>>> ListarUsuariosPorCurso(int cursoId)
        {
            var resultado = await _cursoUsuarioService.ListarUsuariosPorCursoAsync(cursoId);
            return StatusCode(resultado.Codigo, resultado);
        }
    }
}
