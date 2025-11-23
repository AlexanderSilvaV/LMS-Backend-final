using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Services;
using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using LMSBackend.API.Helpers;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    /// <summary>
    /// Controlador específico para que los estudiantes realicen evaluaciones
    /// </summary>
    [ApiController]
    [Route("api/estudiante/evaluaciones")]
    [Authorize(Roles = "Administrador,Docente,Estudiante,Alumno")]
    public class EvaluacionEstudianteController : ControllerBase
    {
        private readonly EvaluacionService _evaluacionService;
        private readonly ILogger<EvaluacionEstudianteController> _logger;

        public EvaluacionEstudianteController(EvaluacionService evaluacionService, ILogger<EvaluacionEstudianteController> logger)
        {
            _evaluacionService = evaluacionService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las evaluaciones disponibles para el estudiante autenticado
        /// </summary>
        /// <returns>Lista de evaluaciones disponibles con información del estado</returns>
        [HttpGet]
        public async Task<ActionResult<ResultadoOperacion<List<EvaluacionEstudianteDTO>>>> ObtenerEvaluacionesDisponibles()
        {
            var resultado = await _evaluacionService.ObtenerEvaluacionesDisponiblesAsync();
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Inicia una nueva sesión de evaluación para el estudiante
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación a iniciar</param>
        /// <returns>Token de sesión y datos para iniciar la evaluación</returns>
        [HttpPost("{evaluacionId}/iniciar")]
        public async Task<ActionResult<ResultadoOperacion<IniciarEvaluacionDTO>>> IniciarEvaluacion(int evaluacionId)
        {
            var resultado = await _evaluacionService.IniciarEvaluacionAsync(evaluacionId);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Obtiene la evaluación completa para que el estudiante la realice
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación</param>
        /// <param name="token">Token de sesión válido</param>
        /// <returns>Evaluación con preguntas y opciones (sin mostrar respuestas correctas)</returns>
        [HttpGet("{evaluacionId}")]
        public async Task<ActionResult<ResultadoOperacion<EvaluacionParaRealizarDTO>>> ObtenerEvaluacionParaRealizar(
            int evaluacionId, 
            [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(ResultadoOperacion<EvaluacionParaRealizarDTO>.Fallo("Token es requerido", 400));
            }

            var resultado = await _evaluacionService.ObtenerEvaluacionParaRealizarAsync(evaluacionId, token);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Envía las respuestas de la evaluación y finaliza la sesión
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación</param>
        /// <param name="dto">Respuestas del estudiante</param>
        /// <param name="token">Token de sesión válido</param>
        /// <returns>Resultado de la evaluación con calificación</returns>
        [HttpPost("{evaluacionId}/responder")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoEvaluacionDTO>>> SubmitRespuestas(
            int evaluacionId,
            [FromBody] SubmitRespuestasDTO dto, 
            [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo("Token es requerido", 400));
            }

            if (dto.EvaluacionId != evaluacionId)
            {
                return BadRequest(ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo("El ID de evaluación no coincide", 400));
            }

            var resultado = await _evaluacionService.SubmitRespuestasConTokenAsync(dto, token);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Obtiene el historial completo de evaluaciones realizadas por el estudiante
        /// </summary>
        /// <returns>Historial de evaluaciones con resultados</returns>
        [HttpGet("historial")]
        public async Task<ActionResult<ResultadoOperacion<List<HistorialEvaluacionDTO>>>> ObtenerHistorialEvaluaciones()
        {
            var resultado = await _evaluacionService.ObtenerHistorialEvaluacionesAsync();
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Obtiene el resultado específico de una evaluación realizada por el estudiante
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación</param>
        /// <param name="numeroIntento">Número del intento</param>
        /// <returns>Resultado detallado de la evaluación</returns>
        [HttpGet("{evaluacionId}/resultado/{numeroIntento}")]
        public async Task<ActionResult<ResultadoOperacion<ResultadoEvaluacionDTO>>> ObtenerMiResultadoEvaluacion(
            int evaluacionId, 
            int numeroIntento)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized(ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo("Usuario no autenticado", 401));
            }

            var resultado = await _evaluacionService.ObtenerResultadoEvaluacionAsync(evaluacionId, usuarioId, numeroIntento);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Obtiene las evaluaciones disponibles para un curso específico
        /// </summary>
        /// <param name="cursoId">ID del curso</param>
        /// <returns>Lista de evaluaciones del curso</returns>
        [HttpGet("curso/{cursoId}")]
        public async Task<ActionResult<ResultadoOperacion<List<EvaluacionEstudianteDTO>>>> ObtenerEvaluacionesPorCurso(int cursoId)
        {
            // Filtrar solo las evaluaciones del curso específico
            var todasLasEvaluaciones = await _evaluacionService.ObtenerEvaluacionesDisponiblesAsync();
            
            if (!todasLasEvaluaciones.OperacionExitosa)
            {
                return StatusCode(todasLasEvaluaciones.Codigo, todasLasEvaluaciones);
            }

            var evaluacionesDelCurso = todasLasEvaluaciones.Dato.Where(e => e.CursoId == cursoId).ToList();
            
            return Ok(ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Exito(evaluacionesDelCurso));
        }

        /// <summary>
        /// Obtiene información básica de una evaluación específica para el estudiante
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación</param>
        /// <returns>Información básica de la evaluación</returns>
        [HttpGet("{evaluacionId}/info")]
        public async Task<ActionResult<ResultadoOperacion<EvaluacionEstudianteDTO>>> ObtenerInformacionEvaluacion(int evaluacionId)
        {
            _logger.LogInformation($"Obteniendo información de evaluación {evaluacionId}");

            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                _logger.LogWarning("Usuario no autenticado al intentar obtener información de evaluación");
                return Unauthorized(ResultadoOperacion<EvaluacionEstudianteDTO>.Fallo("Usuario no autenticado", 401));
            }

            _logger.LogInformation($"Usuario {usuarioId} solicita información de evaluación {evaluacionId}");

            // Obtener todas las evaluaciones disponibles para el estudiante
            var todasLasEvaluaciones = await _evaluacionService.ObtenerEvaluacionesDisponiblesAsync();

            _logger.LogInformation($"Resultado de ObtenerEvaluacionesDisponiblesAsync: OperacionExitosa={todasLasEvaluaciones.OperacionExitosa}, Codigo={todasLasEvaluaciones.Codigo}");

            if (!todasLasEvaluaciones.OperacionExitosa)
            {
                _logger.LogError($"Error al obtener evaluaciones disponibles: {todasLasEvaluaciones.Mensaje}");
                return StatusCode(todasLasEvaluaciones.Codigo, todasLasEvaluaciones);
            }

            // Verificar que la respuesta tenga datos
            if (todasLasEvaluaciones.Dato == null)
            {
                _logger.LogError($"ObtenerEvaluacionesDisponiblesAsync devolvió OperacionExitosa=true pero Dato=null");
                return StatusCode(500, ResultadoOperacion<EvaluacionEstudianteDTO>.Fallo("Error interno del servidor", 500));
            }

            _logger.LogInformation($"Encontradas {todasLasEvaluaciones.Dato.Count} evaluaciones disponibles para el estudiante");

            // Buscar la evaluación específica en las disponibles para el estudiante
            var evaluacion = todasLasEvaluaciones.Dato.FirstOrDefault(e => e.Id == evaluacionId);

            if (evaluacion == null)
            {
                _logger.LogWarning($"Evaluación {evaluacionId} no encontrada en las evaluaciones disponibles del estudiante {usuarioId}");
                return NotFound(ResultadoOperacion<EvaluacionEstudianteDTO>.Fallo("Evaluación no encontrada o no disponible para el estudiante", 404));
            }

            _logger.LogInformation($"Evaluación {evaluacionId} encontrada: {evaluacion.Titulo}");
            return Ok(ResultadoOperacion<EvaluacionEstudianteDTO>.Exito(evaluacion));
        }

        /// <summary>
        /// Marca una sesión de evaluación como abandonada (por ejemplo al cambiar de pestaña o cerrar)
        /// </summary>
        /// <param name="evaluacionId">ID de la evaluación</param>
        /// <param name="token">Token de la sesión a marcar como abandonada</param>
        [HttpPost("{evaluacionId}/abandonar")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> AbandonarSesion(int evaluacionId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(ResultadoOperacion<bool>.Fallo("Token es requerido", 400));
            }

            var resultado = await _evaluacionService.AbandonarSesionAsync(evaluacionId, token);
            return StatusCode(resultado.Codigo, resultado);
        }
    }
}