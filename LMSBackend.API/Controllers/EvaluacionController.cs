using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Services;
using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using LMSBackend.API.Helpers;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EvaluacionController : ControllerBase
    {
        private readonly EvaluacionService _evaluacionService;
        private readonly ILogger<EvaluacionController> _logger;

        public EvaluacionController(EvaluacionService evaluacionService, ILogger<EvaluacionController> logger)
        {
            _evaluacionService = evaluacionService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<EvaluacionDTO>>> CrearEvaluacion([FromBody] EvaluacionCreacionDTO dto)
        {
            var resultado = await _evaluacionService.CrearEvaluacionAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultadoOperacion<EvaluacionDTO>>> ObtenerEvaluacion(int id)
        {
            var resultado = await _evaluacionService.ObtenerEvaluacionPorIdAsync(id);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("curso/{cursoId}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<List<EvaluacionDTO>>>> ObtenerEvaluacionesPorCurso(int cursoId)
        {
            var resultado = await _evaluacionService.ObtenerEvaluacionesPorCursoAsync(cursoId);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("estudiante/curso/{cursoId}")]
        [Authorize(Roles = "Administrador,Docente,Estudiante,Alumno")]
        public async Task<ActionResult<ResultadoOperacion<List<EvaluacionDTO>>>> ObtenerEvaluacionesActivasPorCurso(int cursoId)
        {
            var resultado = await _evaluacionService.ObtenerEvaluacionesActivasPorCursoAsync(cursoId);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<EvaluacionDTO>>> ActualizarEvaluacion(int id, [FromBody] EvaluacionEdicionDTO dto)
        {
            var resultado = await _evaluacionService.ActualizarEvaluacionAsync(id, dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarEvaluacion(int id)
        {
            var resultado = await _evaluacionService.EliminarEvaluacionAsync(id);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("{evaluacionId}/resultado/{usuarioId}/{numeroIntento}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoEvaluacionDTO>>> ObtenerResultadoEvaluacion(
            int evaluacionId, 
            string usuarioId, 
            int numeroIntento)
        {
            var resultado = await _evaluacionService.ObtenerResultadoEvaluacionAsync(evaluacionId, usuarioId, numeroIntento);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpPost("{evaluacionId}/responder-con-token")]
        [Authorize(Roles = "Administrador,Docente,Estudiante,Alumno")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoEvaluacionDTO>>> ResponderEvaluacionConToken(
            int evaluacionId,
            [FromQuery] string token,
            [FromBody] SubmitRespuestasDTO dto)
        {
            var resultado = await _evaluacionService.ResponderEvaluacionConTokenAsync(evaluacionId, token, dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpPost("{evaluacionId}/iniciar")]
        [Authorize(Roles = "Administrador,Docente,Estudiante,Alumno")]
        public async Task<ActionResult<ResultadoOperacion<IniciarEvaluacionDTO>>> IniciarEvaluacion(int evaluacionId)
        {
            var resultado = await _evaluacionService.IniciarEvaluacionAsync(evaluacionId);
            return StatusCode(resultado.Codigo, resultado);
        }

        [HttpGet("{evaluacionId}/resultados")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<List<ResultadoEstudianteEvaluacionDTO>>>> ObtenerResultadosEstudiantesEvaluacion(int evaluacionId)
        {
            var resultado = await _evaluacionService.ObtenerResultadosEstudiantesEvaluacionAsync(evaluacionId);
            return StatusCode(resultado.Codigo, resultado);
        }
    }
}