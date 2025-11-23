using System.Text.Json;
using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMSBackend.API.Services
{
    public class Laboratorio3DLabService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<Laboratorio3DLabService> _logger;
        private static readonly string[] OptionLetters = { "A", "B", "C", "D" };

        private record SesionMetadata(List<int> PreguntasSeleccionadas);

        public Laboratorio3DLabService(ApplicationDbContext context, ILogger<Laboratorio3DLabService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ResultadoOperacion<LaboratorioPreguntasResponseDTO>> ObtenerPoolPreguntasAsync(int evaluacionId)
        {
            var evaluacion = await _context.Evaluaciones
                .Include(e => e.Preguntas)
                    .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(e => e.Id == evaluacionId);

            if (evaluacion == null)
            {
                return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Fallo("Evaluación no encontrada", 404);
            }

            if (!evaluacion.EsLaboratorio3DLab)
            {
                return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Fallo("La evaluación no está configurada como laboratorio 3DLAB", 400);
            }

            var preguntas = evaluacion.Preguntas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .ToList();

            if (preguntas.Count < evaluacion.PreguntasMinimasLaboratorio)
            {
                return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Fallo($"El laboratorio requiere al menos {evaluacion.PreguntasMinimasLaboratorio} preguntas", 409);
            }

            if (evaluacion.PreguntasPorSesionLaboratorio <= 0)
            {
                _logger.LogWarning("Evaluación {EvaluacionId} tiene PreguntasPorSesionLaboratorio inválido: {Cantidad}", evaluacion.Id, evaluacion.PreguntasPorSesionLaboratorio);
                return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Fallo("La cantidad de preguntas por sesión debe ser mayor a cero", 409);
            }

            if (evaluacion.PreguntasPorSesionLaboratorio > preguntas.Count)
            {
                return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Fallo($"El laboratorio está configurado para {evaluacion.PreguntasPorSesionLaboratorio} preguntas por sesión, pero solo hay {preguntas.Count} disponibles", 409);
            }

            var dto = new LaboratorioPreguntasResponseDTO
            {
                EvaluacionId = evaluacion.Id,
                CursoId = evaluacion.CursoId,
                Preguntas = preguntas.Select(MapPregunta).ToList()
            };

            return ResultadoOperacion<LaboratorioPreguntasResponseDTO>.Exito(dto);
        }

        public async Task<ResultadoOperacion<LaboratorioSeleccionResponseDTO>> CrearSesionYSeleccionarPreguntasAsync(int evaluacionId, string alumnoId)
        {
            if (string.IsNullOrWhiteSpace(alumnoId))
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo("AlumnoId es requerido", 400);
            }

            var evaluacion = await _context.Evaluaciones
                .Include(e => e.Preguntas)
                    .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(e => e.Id == evaluacionId);

            if (evaluacion == null)
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo("Evaluación no encontrada", 404);
            }

            if (!evaluacion.EsLaboratorio3DLab)
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo("La evaluación no está configurada como laboratorio 3DLAB", 400);
            }

            var alumnoExiste = await _context.Users.AnyAsync(u => u.Id == alumnoId);
            if (!alumnoExiste)
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo("Alumno no encontrado en LMS", 404);
            }

            var preguntas = evaluacion.Preguntas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .ToList();

            if (preguntas.Count < evaluacion.PreguntasMinimasLaboratorio)
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo($"El laboratorio requiere al menos {evaluacion.PreguntasMinimasLaboratorio} preguntas", 409);
            }

            if (evaluacion.PreguntasPorSesionLaboratorio <= 0)
            {
                _logger.LogWarning("Evaluación {EvaluacionId} tiene PreguntasPorSesionLaboratorio inválido: {Cantidad}", evaluacion.Id, evaluacion.PreguntasPorSesionLaboratorio);
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo("La cantidad de preguntas por sesión debe ser mayor a cero", 409);
            }

            if (evaluacion.PreguntasPorSesionLaboratorio > preguntas.Count)
            {
                return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Fallo($"El laboratorio está configurado para {evaluacion.PreguntasPorSesionLaboratorio} preguntas por sesión, pero solo hay {preguntas.Count} disponibles", 409);
            }

            var selectedQuestions = SelectRandomQuestions(preguntas, evaluacion.PreguntasPorSesionLaboratorio);

            var ultimoIntento = await _context.SesionesEvaluacion
                .Where(s => s.UsuarioId == alumnoId && s.EvaluacionId == evaluacionId)
                .Select(s => (int?)s.NumeroIntento)
                .MaxAsync() ?? 0;

            var numeroIntento = ultimoIntento + 1;

            var metadata = new SesionMetadata(selectedQuestions.Select(p => p.Id).ToList());

            var sesion = new SesionEvaluacion
            {
                UsuarioId = alumnoId,
                EvaluacionId = evaluacionId,
                NumeroIntento = numeroIntento,
                FechaInicio = DateTime.UtcNow,
                Activa = true,
                Completada = false,
                TiempoAgotado = false,
                Metadata = JsonSerializer.Serialize(metadata)
            };

            _context.SesionesEvaluacion.Add(sesion);
            await _context.SaveChangesAsync();

            var response = new LaboratorioSeleccionResponseDTO
            {
                SesionId = sesion.Token,
                EvaluacionId = evaluacion.Id,
                CursoId = evaluacion.CursoId,
                AlumnoId = alumnoId,
                Preguntas = selectedQuestions.Select(MapPregunta).ToList()
            };

            return ResultadoOperacion<LaboratorioSeleccionResponseDTO>.Exito(response);
        }

        public async Task<ResultadoOperacion<LaboratorioResultadosResponseDTO>> RegistrarResultadosAsync(LaboratorioResultadosRequestDTO request)
        {
            var sesion = await _context.SesionesEvaluacion
                .Include(s => s.Evaluacion)
                    .ThenInclude(e => e.Preguntas)
                        .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(s => s.Token == request.SesionId && s.EvaluacionId == request.EvaluacionId);

            if (sesion == null)
            {
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("Sesión no encontrada", 404);
            }

            if (!string.Equals(sesion.UsuarioId, request.AlumnoId, StringComparison.Ordinal))
            {
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("La sesión no corresponde al alumno informado", 400);
            }

            if (!sesion.Activa || sesion.Completada)
            {
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("La sesión ya fue finalizada", 409);
            }

            if (sesion.Evaluacion == null || !sesion.Evaluacion.EsLaboratorio3DLab)
            {
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("La evaluación asociada no es un laboratorio válido", 400);
            }

            var metadata = DeserializeMetadata(sesion.Metadata);
            if (metadata == null)
            {
                _logger.LogWarning("Sesión {SesionId} sin metadata para laboratorio", sesion.Id);
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("Sesión inválida", 500);
            }

            if (request.Respuestas.Count != metadata.PreguntasSeleccionadas.Count)
            {
                return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("El número de respuestas no coincide con el laboratorio asignado", 400);
            }

            var preguntasPorId = sesion.Evaluacion.Preguntas.ToDictionary(p => p.Id);
            var preguntasSeleccionadas = new List<Pregunta>();

            foreach (var preguntaId in metadata.PreguntasSeleccionadas)
            {
                if (!preguntasPorId.TryGetValue(preguntaId, out var pregunta))
                {
                    return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("Pregunta inexistente en la evaluación", 400);
                }

                preguntasSeleccionadas.Add(pregunta);
            }

            var respuestasOrdenadas = request.Respuestas
                .GroupBy(r => r.PreguntaId)
                .Select(g => g.First())
                .ToDictionary(r => r.PreguntaId, r => r);

            foreach (var preguntaId in metadata.PreguntasSeleccionadas)
            {
                if (!respuestasOrdenadas.ContainsKey(preguntaId))
                {
                    return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("Falta respuesta para una pregunta asignada", 400);
                }
            }

            decimal puntajeObtenido = 0;
            decimal puntajeMaximo = 0;
            var detalle = new List<LaboratorioRespuestaDetalleDTO>();
            var respuestasUsuario = new List<RespuestaUsuario>();

            foreach (var pregunta in preguntasSeleccionadas)
            {
                puntajeMaximo += pregunta.Puntos;

                var respuesta = respuestasOrdenadas[pregunta.Id];
                var seleccion = respuesta.Seleccion.Trim().ToUpperInvariant();

                var opcionesMap = MapOpcionesPregunta(pregunta);
                if (!opcionesMap.TryGetValue(seleccion, out var opcionSeleccionada))
                {
                    return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Fallo("Opción seleccionada inválida", 400);
                }

                var esCorrecta = opcionSeleccionada.EsCorrecta;
                var puntos = esCorrecta ? pregunta.Puntos : 0;
                puntajeObtenido += puntos;

                detalle.Add(new LaboratorioRespuestaDetalleDTO
                {
                    PreguntaId = pregunta.Id,
                    Enunciado = pregunta.Texto,
                    Seleccion = seleccion,
                    EsCorrecta = esCorrecta,
                    PuntosOtorgados = puntos,
                    PuntosPregunta = pregunta.Puntos
                });

                respuestasUsuario.Add(new RespuestaUsuario
                {
                    UsuarioId = sesion.UsuarioId,
                    EvaluacionId = sesion.EvaluacionId,
                    PreguntaId = pregunta.Id,
                    OpcionId = opcionSeleccionada.Id,
                    NumeroIntento = sesion.NumeroIntento,
                    FechaRespuesta = DateTime.UtcNow
                });
            }

            var porcentaje = puntajeMaximo > 0 ? (double)puntajeObtenido / (double)puntajeMaximo : 0;
            var calificacion = (decimal)Math.Round((porcentaje * 6) + 1, 1, MidpointRounding.AwayFromZero);
            if (calificacion < 1.0m) calificacion = 1.0m;
            if (calificacion > 7.0m) calificacion = 7.0m;

            sesion.Completada = true;
            sesion.Activa = false;
            sesion.FechaFinalizacion = DateTime.UtcNow;

            var notaExistente = await _context.Notas
                .FirstOrDefaultAsync(n => n.UsuarioId == sesion.UsuarioId && n.EvaluacionId == sesion.EvaluacionId && n.NumeroIntento == sesion.NumeroIntento);

            if (notaExistente == null)
            {
                var nota = new Nota
                {
                    UsuarioId = sesion.UsuarioId,
                    EvaluacionId = sesion.EvaluacionId,
                    Calificacion = calificacion,
                    NumeroIntento = sesion.NumeroIntento,
                    EsNotaFinal = true,
                    EsLaboratorio = true,
                    Observaciones = $"Laboratorio 3DLAB. Puntaje: {puntajeObtenido}/{puntajeMaximo}",
                    FechaCalificacion = DateTime.UtcNow
                };

                _context.Notas.Add(nota);
            }
            else
            {
                notaExistente.Calificacion = calificacion;
                notaExistente.EsLaboratorio = true;
                notaExistente.EsNotaFinal = true;
                notaExistente.Observaciones = $"Laboratorio 3DLAB. Puntaje: {puntajeObtenido}/{puntajeMaximo}";
                notaExistente.FechaCalificacion = DateTime.UtcNow;
            }

            _context.RespuestasUsuario.AddRange(respuestasUsuario);
            await _context.SaveChangesAsync();

            var response = new LaboratorioResultadosResponseDTO
            {
                EvaluacionId = sesion.EvaluacionId,
                AlumnoId = sesion.UsuarioId,
                PuntajeObtenido = puntajeObtenido,
                PuntajeMaximo = puntajeMaximo,
                Calificacion = calificacion,
                Detalle = detalle,
                FechaCalificacion = DateTime.UtcNow
            };

            return ResultadoOperacion<LaboratorioResultadosResponseDTO>.Exito(response);
        }

        private static LaboratorioPreguntaDTO MapPregunta(Pregunta pregunta)
        {
            var opciones = MapOpcionesPregunta(pregunta)
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value.Texto);

            return new LaboratorioPreguntaDTO
            {
                IdPregunta = pregunta.Id,
                Enunciado = pregunta.Texto,
                Opciones = opciones
            };
        }

        private static Dictionary<string, Opcion> MapOpcionesPregunta(Pregunta pregunta)
        {
            var opcionesOrdenadas = pregunta.Opciones
                .OrderBy(o => o.Orden)
                .ThenBy(o => o.Id)
                .Take(OptionLetters.Length)
                .ToList();

            var map = new Dictionary<string, Opcion>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < opcionesOrdenadas.Count && index < OptionLetters.Length; index++)
            {
                map[OptionLetters[index]] = opcionesOrdenadas[index];
            }

            return map;
        }

        private static List<Pregunta> SelectRandomQuestions(List<Pregunta> source, int cantidad)
        {
            if (source.Count <= cantidad)
            {
                return new List<Pregunta>(source);
            }

            var selected = new List<Pregunta>();
            var available = new List<Pregunta>(source);

            while (selected.Count < cantidad && available.Count > 0)
            {
                var idx = Random.Shared.Next(available.Count);
                selected.Add(available[idx]);
                available.RemoveAt(idx);
            }

            return selected;
        }

        private static SesionMetadata? DeserializeMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SesionMetadata>(metadata);
            }
            catch
            {
                return null;
            }
        }
    }
}
