using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LMSBackend.API.Services
{
    public class ValidacionEvaluacionService
    {
        private readonly ApplicationDbContext _context;

        public ValidacionEvaluacionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoOperacion<bool>> ValidarEvaluacionCreacionAsync(EvaluacionCreacionDTO dto)
        {
            // Validar que el curso existe
            var cursoExiste = await _context.Cursos.AnyAsync(c => c.Nrc == dto.CursoId);
            if (!cursoExiste)
            {
                return ResultadoOperacion<bool>.Fallo("El curso especificado no existe", 404);
            }

            // Validar fechas
            if (dto.FechaInicio.HasValue && dto.FechaFin.HasValue)
            {
                if (dto.FechaFin <= dto.FechaInicio)
                {
                    return ResultadoOperacion<bool>.Fallo("La fecha de fin debe ser posterior a la fecha de inicio", 400);
                }
            }

            // Validar preguntas
            if (dto.Preguntas == null || !dto.Preguntas.Any())
            {
                return ResultadoOperacion<bool>.Fallo("La evaluación debe tener al menos una pregunta", 400);
            }

            // Validar cada pregunta
            foreach (var pregunta in dto.Preguntas)
            {
                var validacionPregunta = ValidarPregunta(pregunta);
                if (!validacionPregunta.OperacionExitosa)
                {
                    return validacionPregunta;
                }
            }

            return ResultadoOperacion<bool>.Exito(true, "Validación exitosa");
        }

        private ResultadoOperacion<bool> ValidarPregunta(PreguntaCreacionDTO pregunta)
        {
            // Validar opciones
            if (pregunta.Opciones == null || pregunta.Opciones.Count < 2)
            {
                return ResultadoOperacion<bool>.Fallo($"La pregunta '{pregunta.Texto}' debe tener al menos 2 opciones", 400);
            }

            // Validar que hay exactamente una respuesta correcta
            var opcionesCorrectas = pregunta.Opciones.Count(o => o.EsCorrecta);
            if (opcionesCorrectas == 0)
            {
                return ResultadoOperacion<bool>.Fallo($"La pregunta '{pregunta.Texto}' debe tener al menos una opción correcta", 400);
            }

            if (opcionesCorrectas > 1)
            {
                return ResultadoOperacion<bool>.Fallo($"La pregunta '{pregunta.Texto}' no puede tener más de una opción correcta", 400);
            }

            // Validar órden de opciones
            var ordenesUnicos = pregunta.Opciones.Select(o => o.Orden).Distinct().Count();
            if (ordenesUnicos != pregunta.Opciones.Count)
            {
                return ResultadoOperacion<bool>.Fallo($"Las opciones de la pregunta '{pregunta.Texto}' deben tener órdenes únicos", 400);
            }

            return ResultadoOperacion<bool>.Exito(true, "Pregunta válida");
        }

        public async Task<ResultadoOperacion<bool>> ValidarRespuestaUsuarioAsync(string usuarioId, int evaluacionId, int numeroIntento)
        {
            // Verificar que no haya respuestas duplicadas
            var respuestaExiste = await _context.RespuestasUsuario
                .AnyAsync(r => r.UsuarioId == usuarioId && 
                              r.EvaluacionId == evaluacionId && 
                              r.NumeroIntento == numeroIntento);

            if (respuestaExiste)
            {
                return ResultadoOperacion<bool>.Fallo("Ya existe una respuesta para este intento de evaluación", 409);
            }

            // Verificar límite de intentos
            var intentosRealizados = await _context.RespuestasUsuario
                .Where(r => r.UsuarioId == usuarioId && r.EvaluacionId == evaluacionId)
                .Select(r => r.NumeroIntento)
                .Distinct()
                .CountAsync();

            var evaluacion = await _context.Evaluaciones
                .FirstOrDefaultAsync(e => e.Id == evaluacionId);

            if (evaluacion == null)
            {
                return ResultadoOperacion<bool>.Fallo("Evaluación no encontrada", 404);
            }

            if (intentosRealizados >= evaluacion.IntentosMaximos)
            {
                return ResultadoOperacion<bool>.Fallo("Se ha excedido el número máximo de intentos", 403);
            }

            return ResultadoOperacion<bool>.Exito(true, "Validación exitosa");
        }
    }
}