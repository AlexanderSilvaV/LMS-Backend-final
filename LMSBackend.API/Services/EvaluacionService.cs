using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace LMSBackend.API.Services
{
    public class EvaluacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvaluacionService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;

        public EvaluacionService(
            ApplicationDbContext context,
            ILogger<EvaluacionService> logger,
            IHttpContextAccessor httpContextAccessor,
            EmailService emailService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
        }

        public async Task<ResultadoOperacion<EvaluacionDTO>> CrearEvaluacionAsync(EvaluacionCreacionDTO dto)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null)
            {
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "No hay contexto", 400);
            }

            var usuario = contexto.User;
            var usuarioId = usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            var existeCurso = await _context.Cursos.AnyAsync(c => c.Nrc == dto.CursoId);
            if (!existeCurso)
            {
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Curso no encontrado", 404);
            }

                        // Determinar el tipo de evaluación (solo manual soportado)
            var tipoEvaluacion = TipoEvaluacion.Manual;

            // Validar que tenga preguntas (excepto para laboratorios 3DLAB que pueden crearse sin preguntas iniciales)
            if (!dto.EsLaboratorio3DLab && (dto.Preguntas == null || !dto.Preguntas.Any()))
            {
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "La evaluación debe tener al menos una pregunta", 400);
            }

            // Validar preguntas si existen
            if (dto.Preguntas != null)
            {
                foreach (var preguntaDto in dto.Preguntas)
            {
                if (string.IsNullOrWhiteSpace(preguntaDto.Texto))
                {
                    return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Todas las preguntas deben tener texto", 400);
                }

                if (preguntaDto.Opciones == null || preguntaDto.Opciones.Count < 2)
                {
                    return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Cada pregunta debe tener al menos 2 opciones", 400);
                }

                    if (!preguntaDto.Opciones.Any(o => o.EsCorrecta))
                    {
                        return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Cada pregunta debe tener al menos una respuesta correcta", 400);
                    }
                }
            }

            try
            {
                var evaluacion = new Evaluacion
                {
                    Titulo = dto.Titulo,
                    Descripcion = dto.Descripcion,
                    CursoId = dto.CursoId,
                    DocenteId = usuarioId,
                    FechaInicio = dto.FechaInicio,
                    FechaFin = dto.FechaFin,
                    TiempoLimiteMins = dto.TiempoLimiteMins,
                    IntentosMaximos = dto.IntentosMaximos,
                    EsLaboratorio3DLab = dto.EsLaboratorio3DLab,
                    PreguntasMinimasLaboratorio = dto.PreguntasMinimasLaboratorio,
                    PreguntasPorSesionLaboratorio = dto.PreguntasPorSesionLaboratorio
                };

                _context.Evaluaciones.Add(evaluacion);
                await _context.SaveChangesAsync();

                // Procesar según el tipo
                if (tipoEvaluacion == TipoEvaluacion.Manual && dto.Preguntas != null)
                {
                    // Crear preguntas manuales
                    foreach (var preguntaDto in dto.Preguntas)
                    {
                        var pregunta = new Pregunta
                        {
                            Texto = preguntaDto.Texto,
                            Orden = preguntaDto.Orden,
                            Puntos = preguntaDto.Puntos,
                            EvaluacionId = evaluacion.Id
                        };

                        _context.Preguntas.Add(pregunta);
                        await _context.SaveChangesAsync();

                        foreach (var opcionDto in preguntaDto.Opciones)
                        {
                            var opcion = new Opcion
                            {
                                Texto = opcionDto.Texto,
                                EsCorrecta = opcionDto.EsCorrecta,
                                Orden = opcionDto.Orden,
                                PreguntaId = pregunta.Id
                            };

                            _context.Opciones.Add(opcion);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Enviar notificaciones por email a los estudiantes del curso
                try
                {
                    var curso = await _context.Cursos.FindAsync(dto.CursoId);

                    // Obtener todos los estudiantes del curso
                    var estudiantesDelCurso = await _context.CursoUsuarios
                        .Include(cu => cu.Usuario)
                        .Where(cu => cu.CursoId == dto.CursoId && cu.RolEnCurso == RolEnCurso.Alumno)
                        .Select(cu => cu.Usuario)
                        .ToListAsync();

                    var enlaceEvaluacion = $"http://135.148.148.88:3000/evaluaciones/{evaluacion.Id}";

                    foreach (var estudiante in estudiantesDelCurso)
                    {
                        if (!string.IsNullOrEmpty(estudiante.Email))
                        {
                            await _emailService.EnviarNotificacionNuevaEvaluacionAsync(
                                destinatario: estudiante.Email,
                                nombreUsuario: estudiante.Nombre,
                                nombreCurso: curso?.Nombre ?? "Curso",
                                tituloEvaluacion: evaluacion.Titulo,
                                fechaInicio: evaluacion.FechaInicio ?? DateTime.UtcNow,
                                fechaFin: evaluacion.FechaFin ?? DateTime.UtcNow.AddDays(7),
                                enlaceEvaluacion: enlaceEvaluacion
                            );
                        }
                    }

                    _logger.LogInformation($"Notificaciones enviadas para nueva evaluación: {evaluacion.Titulo} ({estudiantesDelCurso.Count} destinatarios)");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al enviar notificaciones para evaluación {evaluacion.Id}: {ex.Message}");
                    // No bloqueamos la creación de la evaluación si falla el envío de emails
                }

                var evaluacionCreada = await ObtenerEvaluacionPorIdAsync(evaluacion.Id);
                return evaluacionCreada;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear evaluación");
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<EvaluacionDTO>> ObtenerEvaluacionPorIdAsync(int id)
        {
            try
            {
                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Curso)
                    .Include(e => e.Docente)
                    .Include(e => e.Preguntas)
                        .ThenInclude(p => p.Opciones)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Evaluación no encontrada", 404);
                }

                var evaluacionDto = new EvaluacionDTO
                {
                    Id = evaluacion.Id,
                    Titulo = evaluacion.Titulo,
                    Descripcion = evaluacion.Descripcion,
                    CursoId = evaluacion.CursoId,
                    CursoNombre = evaluacion.Curso.Nombre,
                    DocenteId = evaluacion.DocenteId,
                    DocenteNombre = evaluacion.Docente.Nombre,
                    FechaCreacion = evaluacion.FechaCreacion,
                    FechaInicio = evaluacion.FechaInicio,
                    FechaFin = evaluacion.FechaFin,
                    TiempoLimiteMins = evaluacion.TiempoLimiteMins,
                    Activa = evaluacion.Activa,
                    IntentosMaximos = evaluacion.IntentosMaximos,
                    TotalPreguntas = evaluacion.Preguntas.Count,
                    EsLaboratorio3DLab = evaluacion.EsLaboratorio3DLab,
                    PreguntasMinimasLaboratorio = evaluacion.PreguntasMinimasLaboratorio,
                    PreguntasPorSesionLaboratorio = evaluacion.PreguntasPorSesionLaboratorio,
                    Preguntas = evaluacion.Preguntas.Select(p => new PreguntaDTO
                    {
                        Id = p.Id,
                        Texto = p.Texto,
                        Orden = p.Orden,
                        Puntos = p.Puntos,
                        EvaluacionId = p.EvaluacionId,
                        Opciones = p.Opciones.Select(o => new OpcionDTO
                        {
                            Id = o.Id,
                            Texto = o.Texto,
                            EsCorrecta = o.EsCorrecta,
                            Orden = o.Orden,
                            PreguntaId = o.PreguntaId
                        }).OrderBy(o => o.Orden).ToList()
                    }).OrderBy(p => p.Orden).ToList()
                };

                return ResultadoOperacion<EvaluacionDTO>.Exito(evaluacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evaluación");
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<EvaluacionDTO>>> ObtenerEvaluacionesPorCursoAsync(int cursoId)
        {
            try
            {
                var evaluaciones = await _context.Evaluaciones
                    .Include(e => e.Curso)
                    .Include(e => e.Docente)
                    .Include(e => e.Preguntas)
                    .Where(e => e.CursoId == cursoId)
                    .OrderByDescending(e => e.FechaCreacion)
                    .ToListAsync();

                var evaluacionesDto = evaluaciones.Select(e => new EvaluacionDTO
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descripcion = e.Descripcion,
                    CursoId = e.CursoId,
                    CursoNombre = e.Curso.Nombre,
                    DocenteId = e.DocenteId,
                    DocenteNombre = e.Docente.Nombre,
                    FechaCreacion = e.FechaCreacion,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin,
                    TiempoLimiteMins = e.TiempoLimiteMins,
                    Activa = e.Activa,
                    IntentosMaximos = e.IntentosMaximos,
                    TotalPreguntas = e.Preguntas.Count,
                    EsLaboratorio3DLab = e.EsLaboratorio3DLab,
                    PreguntasMinimasLaboratorio = e.PreguntasMinimasLaboratorio,
                    PreguntasPorSesionLaboratorio = e.PreguntasPorSesionLaboratorio
                }).ToList();

                return ResultadoOperacion<List<EvaluacionDTO>>.Exito(evaluacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evaluaciones por curso");
                return ResultadoOperacion<List<EvaluacionDTO>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<EvaluacionDTO>>> ObtenerEvaluacionesActivasPorCursoAsync(int cursoId)
        {
            try
            {
                // Temporalmente, obtener TODAS las evaluaciones del curso para debug
                var evaluaciones = await _context.Evaluaciones
                    .Include(e => e.Curso)
                    .Include(e => e.Docente)
                    .Include(e => e.Preguntas)
                    .Where(e => e.CursoId == cursoId)
                    .OrderByDescending(e => e.FechaCreacion)
                    .ToListAsync();

                _logger.LogInformation($"Encontradas {evaluaciones.Count} evaluaciones totales para curso {cursoId}");
                
                // Log de cada evaluación para debug
                foreach (var eval in evaluaciones)
                {
                    _logger.LogInformation($"Evaluación: {eval.Titulo}, Activa: {eval.Activa}, Inicio: {eval.FechaInicio}, Fin: {eval.FechaFin}");
                }

                var evaluacionesDto = evaluaciones.Select(e => new EvaluacionDTO
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descripcion = e.Descripcion,
                    CursoId = e.CursoId,
                    CursoNombre = e.Curso.Nombre,
                    DocenteId = e.DocenteId,
                    DocenteNombre = e.Docente.Nombre,
                    FechaCreacion = e.FechaCreacion,
                    FechaInicio = e.FechaInicio,
                    FechaFin = e.FechaFin,
                    TiempoLimiteMins = e.TiempoLimiteMins,
                    Activa = e.Activa,
                    IntentosMaximos = e.IntentosMaximos,
                    TotalPreguntas = e.Preguntas.Count,
                    EsLaboratorio3DLab = e.EsLaboratorio3DLab,
                    PreguntasMinimasLaboratorio = e.PreguntasMinimasLaboratorio,
                    PreguntasPorSesionLaboratorio = e.PreguntasPorSesionLaboratorio
                }).ToList();

                return ResultadoOperacion<List<EvaluacionDTO>>.Exito(evaluacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evaluaciones activas por curso");
                return ResultadoOperacion<List<EvaluacionDTO>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<EvaluacionDTO>> ActualizarEvaluacionAsync(int id, EvaluacionEdicionDTO dto)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var evaluacion = await _context.Evaluaciones
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Evaluación no encontrada", 404);
                }

                if (evaluacion.DocenteId != usuarioId)
                {
                    return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "No tiene permisos para editar esta evaluación", 403);
                }

                evaluacion.Titulo = dto.Titulo;
                evaluacion.Descripcion = dto.Descripcion;
                evaluacion.FechaInicio = dto.FechaInicio;
                evaluacion.FechaFin = dto.FechaFin;
                evaluacion.TiempoLimiteMins = dto.TiempoLimiteMins;
                evaluacion.Activa = dto.Activa;
                evaluacion.IntentosMaximos = dto.IntentosMaximos;
                evaluacion.EsLaboratorio3DLab = dto.EsLaboratorio3DLab;
                evaluacion.PreguntasMinimasLaboratorio = dto.PreguntasMinimasLaboratorio;
                evaluacion.PreguntasPorSesionLaboratorio = dto.PreguntasPorSesionLaboratorio;

                await _context.SaveChangesAsync();

                return await ObtenerEvaluacionPorIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar evaluación");
                return ResultadoOperacion<EvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarEvaluacionAsync(int id)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Preguntas)
                        .ThenInclude(p => p.Opciones)
                    .Include(e => e.RespuestasUsuario)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<bool>.Fallo(mensaje: "Evaluación no encontrada", 404);
                }

                if (evaluacion.DocenteId != usuarioId)
                {
                    return ResultadoOperacion<bool>.Fallo(mensaje: "No tiene permisos para eliminar esta evaluación", 403);
                }

                _context.RespuestasUsuario.RemoveRange(evaluacion.RespuestasUsuario);
                
                foreach (var pregunta in evaluacion.Preguntas)
                {
                    _context.Opciones.RemoveRange(pregunta.Opciones);
                }
                
                _context.Preguntas.RemoveRange(evaluacion.Preguntas);
                _context.Evaluaciones.Remove(evaluacion);

                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.Exito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar evaluación");
                return ResultadoOperacion<bool>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<ResultadoEvaluacionDTO>> SubmitRespuestasAsync(SubmitRespuestasDTO dto)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioId))
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Preguntas)
                        .ThenInclude(p => p.Opciones)
                    .FirstOrDefaultAsync(e => e.Id == dto.EvaluacionId);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Evaluación no encontrada", 404);
                }

                if (!evaluacion.Activa)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Evaluación no está activa", 400);
                }

                var intentosExistentes = await _context.RespuestasUsuario
                    .Where(r => r.EvaluacionId == dto.EvaluacionId && r.UsuarioId == usuarioId)
                    .Select(r => r.NumeroIntento)
                    .Distinct()
                    .CountAsync();

                if (intentosExistentes >= evaluacion.IntentosMaximos)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Ha alcanzado el número máximo de intentos", 400);
                }

                var numeroIntento = intentosExistentes + 1;

                // Validación anti-trampa básica: Verificar integridad de respuestas
                var preguntasEvaluacion = evaluacion.Preguntas.ToList();
                var preguntasIdsValidas = preguntasEvaluacion.Select(p => p.Id).ToHashSet();

                foreach (var respuesta in dto.Respuestas)
                {
                    if (!preguntasIdsValidas.Contains(respuesta.PreguntaId))
                    {
                        _logger.LogWarning($"[ANTI-TRAMPA] Pregunta inválida en SubmitRespuestasAsync - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, PreguntaId: {respuesta.PreguntaId}");
                        return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                    }

                    var pregunta = preguntasEvaluacion.First(p => p.Id == respuesta.PreguntaId);
                    var opcionesIdsValidas = pregunta.Opciones.Select(o => o.Id).ToHashSet();

                    if (!opcionesIdsValidas.Contains(respuesta.OpcionId))
                    {
                        _logger.LogWarning($"[ANTI-TRAMPA] Opción inválida en SubmitRespuestasAsync - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, PreguntaId: {respuesta.PreguntaId}, OpcionId: {respuesta.OpcionId}");
                        return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                    }
                }

                // Validación anti-trampa: Verificar que no haya respuestas duplicadas
                var preguntasRespondidas = dto.Respuestas.Select(r => r.PreguntaId).ToList();
                if (preguntasRespondidas.Count != preguntasRespondidas.Distinct().Count())
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Respuestas duplicadas en SubmitRespuestasAsync - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                }

                // Validación anti-trampa: Verificar que se respondieron todas las preguntas
                if (preguntasRespondidas.Count != preguntasEvaluacion.Count)
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] No todas las preguntas respondidas en SubmitRespuestasAsync - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Respondidas: {preguntasRespondidas.Count}, Total: {preguntasEvaluacion.Count}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Debe responder todas las preguntas", 400);
                }

                var respuestasUsuario = dto.Respuestas.Select(r => new RespuestaUsuario
                {
                    UsuarioId = usuarioId,
                    EvaluacionId = dto.EvaluacionId,
                    PreguntaId = r.PreguntaId,
                    OpcionId = r.OpcionId,
                    NumeroIntento = numeroIntento
                }).ToList();

                _context.RespuestasUsuario.AddRange(respuestasUsuario);
                await _context.SaveChangesAsync();

                return await ObtenerResultadoEvaluacionAsync(dto.EvaluacionId, usuarioId, numeroIntento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar respuestas");
                return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<ResultadoEvaluacionDTO>> ObtenerResultadoEvaluacionAsync(int evaluacionId, string usuarioId, int numeroIntento)
        {
            try
            {
                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Preguntas)
                        .ThenInclude(p => p.Opciones)
                    .FirstOrDefaultAsync(e => e.Id == evaluacionId);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Evaluación no encontrada", 404);
                }

                var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Id == usuarioId);

                var respuestas = await _context.RespuestasUsuario
                    .Include(r => r.Pregunta)
                    .Include(r => r.Opcion)
                    .Where(r => r.EvaluacionId == evaluacionId && r.UsuarioId == usuarioId && r.NumeroIntento == numeroIntento)
                    .ToListAsync();

                var puntajeMaximo = evaluacion.Preguntas?.Sum(p => p.Puntos) ?? 0;
                var puntajeObtenido = 0;

                var detalleRespuestas = respuestas.Select(r =>
                {
                    if (r.Pregunta == null)
                    {
                        return new DetalleRespuestaDTO
                        {
                            PreguntaId = r.PreguntaId,
                            PreguntaTexto = "Pregunta no disponible",
                            OpcionSeleccionadaId = r.OpcionId,
                            OpcionSeleccionadaTexto = r.Opcion?.Texto ?? "Opción no encontrada",
                            EsCorrecta = false,
                            PuntosObtenidos = 0
                        };
                    }

                    var esCorrecta = r.Opcion?.EsCorrecta ?? false;
                    if (esCorrecta)
                    {
                        puntajeObtenido += r.Pregunta.Puntos;
                    }

                    return new DetalleRespuestaDTO
                    {
                        PreguntaId = r.PreguntaId,
                        PreguntaTexto = r.Pregunta.Texto,
                        OpcionSeleccionadaId = r.OpcionId,
                        OpcionSeleccionadaTexto = r.Opcion?.Texto ?? "Opción no encontrada",
                        EsCorrecta = esCorrecta,
                        PuntosObtenidos = esCorrecta ? r.Pregunta.Puntos : 0
                    };
                }).ToList();

                var porcentaje = puntajeMaximo > 0 ? (double)puntajeObtenido / puntajeMaximo * 100 : 0;

                // Convertir porcentaje a escala chilena (1.0 - 7.0)
                var calificacion = Math.Round((porcentaje / 100) * 6 + 1, 1);
                if (calificacion < 1.0) calificacion = 1.0;
                if (calificacion > 7.0) calificacion = 7.0;

                // Guardar la nota en la tabla Notas
                var notaExistente = await _context.Notas
                    .FirstOrDefaultAsync(n => n.EvaluacionId == evaluacionId && 
                                            n.UsuarioId == usuarioId && 
                                            n.NumeroIntento == numeroIntento);

                if (notaExistente == null)
                {
                    var nuevaNota = new Nota
                    {
                        UsuarioId = usuarioId,
                        EvaluacionId = evaluacionId,
                        Calificacion = (decimal)calificacion,
                        NumeroIntento = numeroIntento,
                        EsNotaFinal = true, // Por defecto, la primera nota es final
                        Observaciones = $"Evaluación completada automáticamente. Puntaje: {puntajeObtenido}/{puntajeMaximo} ({porcentaje:F1}%)",
                        FechaCalificacion = DateTime.UtcNow
                    };

                    _context.Notas.Add(nuevaNota);
                    await _context.SaveChangesAsync();
                }

                var resultado = new ResultadoEvaluacionDTO
                {
                    EvaluacionId = evaluacionId,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuario?.Nombre ?? "",
                    PuntajeObtenido = puntajeObtenido,
                    PuntajeMaximo = puntajeMaximo,
                    Porcentaje = Math.Round(porcentaje, 2),
                    FechaCompletado = respuestas.FirstOrDefault()?.FechaRespuesta ?? DateTime.UtcNow,
                    NumeroIntento = numeroIntento,
                    DetalleRespuestas = detalleRespuestas
                };

                return ResultadoOperacion<ResultadoEvaluacionDTO>.Exito(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resultado de evaluación");
                return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<EvaluacionEstudianteDTO>>> ObtenerEvaluacionesDisponiblesAsync()
        {
            _logger.LogInformation("[DEBUG] Iniciando ObtenerEvaluacionesDisponiblesAsync");

            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                if (contexto == null)
                {
                    _logger.LogError("[DEBUG] HttpContext es null");
                    return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Fallo(mensaje: "Contexto HTTP no disponible", 500);
                }

                var usuario = contexto.User;
                if (usuario == null)
                {
                    _logger.LogError("[DEBUG] Usuario en contexto es null");
                    return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Fallo(mensaje: "Usuario no encontrado en contexto", 500);
                }

                var usuarioId = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                _logger.LogInformation($"[DEBUG] Usuario ID extraído del token: {usuarioId ?? "null"}");
                _logger.LogInformation($"[DEBUG] Claims disponibles: {string.Join(", ", usuario.Claims.Select(c => $"{c.Type}={c.Value}"))}");

                if (string.IsNullOrEmpty(usuarioId))
                {
                    _logger.LogWarning("[DEBUG] Usuario no autenticado - no se encontró claim de ID");
                    return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var cursosDelUsuario = await _context.CursoUsuarios
                    .Where(cu => cu.UsuarioId == usuarioId)
                    .Select(cu => cu.CursoId)
                    .ToListAsync();

                _logger.LogInformation($"[DEBUG] Usuario {usuarioId} está inscrito en {cursosDelUsuario.Count} cursos: {string.Join(", ", cursosDelUsuario)}");

                // Verificar si el usuario existe en la base de datos
                var usuarioExiste = await _context.Users.AnyAsync(u => u.Id == usuarioId);
                _logger.LogInformation($"[DEBUG] Usuario {usuarioId} existe en la base de datos: {usuarioExiste}");

                if (!usuarioExiste)
                {
                    _logger.LogError($"[DEBUG] Usuario {usuarioId} no existe en la base de datos");
                    return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Fallo(mensaje: "Usuario no encontrado", 404);
                }

                // Verificar las relaciones curso-usuario
                var relacionesCursoUsuario = await _context.CursoUsuarios
                    .Where(cu => cu.UsuarioId == usuarioId)
                    .ToListAsync();
                _logger.LogInformation($"[DEBUG] Encontradas {relacionesCursoUsuario.Count} relaciones curso-usuario para {usuarioId}");

                if (cursosDelUsuario.Count == 0)
                {
                    _logger.LogWarning($"[DEBUG] Usuario {usuarioId} no está inscrito en ningún curso");
                    return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Exito(new List<EvaluacionEstudianteDTO>());
                }

                var evaluacionesConPreguntas = await _context.Evaluaciones
                    .Include(e => e.Curso)
                    .Include(e => e.Docente)
                    .Include(e => e.Preguntas)
                    .Where(e => cursosDelUsuario.Contains(e.CursoId) && e.Activa)
                    .OrderByDescending(e => e.FechaCreacion)
                    .Select(e => new {
                        Evaluacion = e,
                        TotalPreguntas = e.Preguntas.Count
                    })
                    .ToListAsync();

                _logger.LogInformation($"[DEBUG] Encontradas {evaluacionesConPreguntas.Count} evaluaciones activas para los cursos del usuario");
                foreach (var item in evaluacionesConPreguntas)
                {
                    _logger.LogInformation($"[DEBUG] Evaluación: {item.Evaluacion.Titulo} (ID: {item.Evaluacion.Id}, Curso: {item.Evaluacion.Curso.Nombre}, Activa: {item.Evaluacion.Activa}, Preguntas: {item.TotalPreguntas})");
                }

                var respuestasUsuario = await _context.RespuestasUsuario
                    .Where(r => r.UsuarioId == usuarioId)
                    .GroupBy(r => new { r.EvaluacionId, r.NumeroIntento })
                    .Select(g => new {
                        EvaluacionId = g.Key.EvaluacionId,
                        NumeroIntento = g.Key.NumeroIntento,
                        FechaRespuesta = g.Max(r => r.FechaRespuesta),
                        PuntajeObtenido = g.Sum(r => r.Opcion.EsCorrecta ? r.Pregunta.Puntos : 0),
                        PuntajeMaximo = g.Sum(r => r.Pregunta.Puntos)
                    })
                    .ToListAsync();

                _logger.LogInformation($"Encontradas {respuestasUsuario.Count} respuestas del usuario");

                // Obtener sesiones activas del usuario
                // Nota: FechaLimite es una propiedad calculada en el modelo; EF Core no puede traducirla a SQL.
                // Por eso traemos las sesiones candidatas y filtramos FechaLimite en memoria.
                var sesionesCandidatas = await _context.SesionesEvaluacion
                    .Where(s => s.UsuarioId == usuarioId && s.Activa && !s.Completada)
                    .Include(s => s.Evaluacion)
                    .ToListAsync();

                var sesionesActivas = sesionesCandidatas
                    .Where(s => s.FechaLimite > DateTime.UtcNow)
                    .ToList();

                _logger.LogInformation($"Encontradas {sesionesActivas.Count} sesiones activas del usuario");

                var evaluacionesDto = evaluacionesConPreguntas.Select(item =>
                {
                    var e = item.Evaluacion;
                    var intentosRealizados = respuestasUsuario.Count(r => r.EvaluacionId == e.Id);
                    var mejorResultado = respuestasUsuario
                        .Where(r => r.EvaluacionId == e.Id)
                        .OrderByDescending(r => (double)r.PuntajeObtenido / r.PuntajeMaximo * 100)
                        .FirstOrDefault();

                    // Buscar si hay una sesión activa para esta evaluación
                    var sesionActiva = sesionesActivas.FirstOrDefault(s => s.EvaluacionId == e.Id);
                    UltimoIntentoDTO? ultimoIntento = null;
                    
                    if (sesionActiva != null)
                    {
                        ultimoIntento = new UltimoIntentoDTO
                        {
                            FechaInicio = sesionActiva.FechaInicio,
                            Estado = "En Progreso"
                        };
                        _logger.LogInformation($"[DEBUG] Evaluación {e.Id} tiene sesión activa - Estado: En Progreso");
                    }
                    else if (intentosRealizados > 0)
                    {
                        // Si no hay sesión activa pero hay intentos realizados, el último estado es completado
                        var ultimoResultado = respuestasUsuario
                            .Where(r => r.EvaluacionId == e.Id)
                            .OrderByDescending(r => r.NumeroIntento)
                            .FirstOrDefault();
                            
                        if (ultimoResultado != null)
                        {
                            ultimoIntento = new UltimoIntentoDTO
                            {
                                FechaInicio = ultimoResultado.FechaRespuesta,
                                Calificacion = ultimoResultado.PuntajeMaximo > 0 ? (double)ultimoResultado.PuntajeObtenido / ultimoResultado.PuntajeMaximo * 100 : 0,
                                Estado = "Completado"
                            };
                        }
                    }

                    var ahora = DateTime.UtcNow;
                    var fechaInicioUtc = e.FechaInicio?.ToUniversalTime();
                    var fechaFinUtc = e.FechaFin?.ToUniversalTime();
                    var fechaInicioCheck = !fechaInicioUtc.HasValue || ahora >= fechaInicioUtc.Value;
                    var fechaFinCheck = !fechaFinUtc.HasValue || ahora <= fechaFinUtc.Value;
                    var estaEnTiempo = fechaInicioCheck && fechaFinCheck;

                    var puedeRealizar = estaEnTiempo && intentosRealizados < e.IntentosMaximos;

                    _logger.LogInformation($"[DEBUG] Evaluación {e.Id} '{e.Titulo}':");
                    _logger.LogInformation($"  - Intentos: {intentosRealizados}/{e.IntentosMaximos}");
                    _logger.LogInformation($"  - Fechas: Inicio={e.FechaInicio?.ToString() ?? "null"} (UTC: {fechaInicioUtc?.ToString() ?? "null"}), Fin={e.FechaFin?.ToString() ?? "null"} (UTC: {fechaFinUtc?.ToString() ?? "null"}), Ahora={ahora}");
                    _logger.LogInformation($"  - Fecha checks: InicioOK={fechaInicioCheck}, FinOK={fechaFinCheck}");
                    _logger.LogInformation($"  - EstaEnTiempo: {estaEnTiempo}");
                    _logger.LogInformation($"  - PuedeRealizarEvaluacion: {puedeRealizar}");

                    var totalPreguntas = item.TotalPreguntas;
                    _logger.LogInformation($"[DEBUG] Evaluación {e.Id} '{e.Titulo}': TotalPreguntas = {totalPreguntas}");

                    return new EvaluacionEstudianteDTO
                    {
                        Id = e.Id,
                        Titulo = e.Titulo,
                        Descripcion = e.Descripcion,
                        CursoId = e.CursoId,
                        CursoNombre = e.Curso.Nombre,
                        DocenteNombre = e.Docente?.Nombre ?? "Sin asignar",
                        FechaCreacion = e.FechaCreacion,
                        FechaInicio = e.FechaInicio,
                        FechaFin = e.FechaFin,
                        TiempoLimiteMins = e.TiempoLimiteMins,
                        Activa = e.Activa,
                        IntentosMaximos = e.IntentosMaximos,
                        TotalPreguntas = totalPreguntas,
                        IntentosRealizados = intentosRealizados,
                        PuedeRealizarEvaluacion = puedeRealizar,
                        YaCompletada = intentosRealizados >= e.IntentosMaximos,
                        UltimaFechaRealizada = respuestasUsuario
                            .Where(r => r.EvaluacionId == e.Id)
                            .Max(r => (DateTime?)r.FechaRespuesta),
                        MejorPorcentaje = mejorResultado != null ? (double)mejorResultado.PuntajeObtenido / mejorResultado.PuntajeMaximo * 100 : null,
                        EstaEnTiempo = estaEnTiempo,
                        Curso = new CursoBasicoDTO
                        {
                            Nombre = e.Curso.Nombre,
                            Nrc = e.Curso.Nrc.ToString()
                        },
                        UltimoIntento = ultimoIntento
                    };
                }).ToList();

                _logger.LogInformation($"[DEBUG] Devolviendo {evaluacionesDto.Count} evaluaciones DTO al cliente");
                foreach (var dto in evaluacionesDto)
                {
                    _logger.LogInformation($"[DEBUG] DTO: {dto.Titulo} - Intentos: {dto.IntentosRealizados}/{dto.IntentosMaximos}, Mejor: {dto.MejorPorcentaje}%, YaCompletada: {dto.YaCompletada}");
                }

                return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Exito(evaluacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evaluaciones disponibles");
                return ResultadoOperacion<List<EvaluacionEstudianteDTO>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<IniciarEvaluacionDTO>> IniciarEvaluacionAsync(int evaluacionId)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation($"Iniciando evaluación {evaluacionId} para usuario {usuarioId}");

                if (string.IsNullOrEmpty(usuarioId))
                {
                    _logger.LogWarning("Usuario no autenticado al intentar iniciar evaluación");
                    return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var maxRetries = 3;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    // Use a serializable transaction to avoid race conditions when two tabs try to create a session simultaneously.
                    using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            var evaluacion = await _context.Evaluaciones
                                .FirstOrDefaultAsync(e => e.Id == evaluacionId);

                            if (evaluacion == null)
                            {
                                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Evaluación no encontrada", 404);
                            }

                            if (!evaluacion.Activa)
                            {
                                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Evaluación no está activa", 400);
                            }

                            var ahora = DateTime.UtcNow;
                            var fechaInicioUtc = evaluacion.FechaInicio?.ToUniversalTime();
                            var fechaFinUtc = evaluacion.FechaFin?.ToUniversalTime();

                            if (fechaInicioUtc.HasValue && ahora < fechaInicioUtc.Value)
                            {
                                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "La evaluación aún no ha comenzado", 400);
                            }

                            if (fechaFinUtc.HasValue && ahora > fechaFinUtc.Value)
                            {
                                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "La evaluación ha finalizado", 400);
                            }

                            // Recompute attempts inside the transaction
                            var intentosRealizados = await _context.RespuestasUsuario
                                .Where(r => r.EvaluacionId == evaluacionId && r.UsuarioId == usuarioId)
                                .Select(r => r.NumeroIntento)
                                .Distinct()
                                .CountAsync();

                            _logger.LogInformation($"Usuario {usuarioId} - Evaluación {evaluacionId}: Intentos realizados: {intentosRealizados}, Máximo permitido: {evaluacion.IntentosMaximos}");

                            if (intentosRealizados >= evaluacion.IntentosMaximos)
                            {
                                _logger.LogWarning($"Usuario {usuarioId} ha alcanzado el máximo de intentos ({intentosRealizados}/{evaluacion.IntentosMaximos}) para evaluación {evaluacionId}");
                                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Ha alcanzado el número máximo de intentos", 400);
                            }

                            // Validación anti-trampa: Verificar que el usuario no tenga otras sesiones activas en evaluaciones diferentes
                            var otrasSesionesActivas = await _context.SesionesEvaluacion
                                .Where(s => s.UsuarioId == usuarioId && s.Activa && s.EvaluacionId != evaluacionId)
                                .ToListAsync();

                            if (otrasSesionesActivas.Any())
                            {
                                _logger.LogWarning($"[ANTI-TRAMPA] Usuario {usuarioId} tiene {otrasSesionesActivas.Count} sesiones activas en otras evaluaciones. Evaluaciones activas: {string.Join(", ", otrasSesionesActivas.Select(s => s.EvaluacionId))}");
                                // Opcional: Cerrar las otras sesiones automáticamente
                                foreach (var sesionActiva in otrasSesionesActivas)
                                {
                                    sesionActiva.Activa = false;
                                    sesionActiva.TiempoAgotado = true;
                                    sesionActiva.FechaFinalizacion = DateTime.UtcNow;
                                }
                                await _context.SaveChangesAsync();
                                _logger.LogInformation($"[ANTI-TRAMPA] Cerradas {otrasSesionesActivas.Count} sesiones activas previas para usuario {usuarioId}");
                            }

                            // Re-check for an existing active session inside the transaction
                            var sesionExistente = await _context.SesionesEvaluacion
                                .FirstOrDefaultAsync(s => s.EvaluacionId == evaluacionId && s.UsuarioId == usuarioId && s.Activa);

                            if (sesionExistente != null)
                            {
                                _logger.LogInformation($"Usuario {usuarioId} tiene una sesión activa para evaluación {evaluacionId}. Sesión ID: {sesionExistente.Id}, Vencida: {sesionExistente.EstaVencida}");

                                if (sesionExistente.EstaVencida)
                                {
                                    _logger.LogInformation($"Marcando sesión {sesionExistente.Id} como inactiva por tiempo agotado");
                                    sesionExistente.Activa = false;
                                    sesionExistente.TiempoAgotado = true;
                                    sesionExistente.FechaFinalizacion = DateTime.UtcNow;
                                    await _context.SaveChangesAsync();
                                    await transaction.CommitAsync();
                                    // Loop will retry to create a new session if appropriate
                                    continue;
                                }
                                else
                                {
                                    // Return existing active session (idempotent)
                                    var iniciarDtoExistente = new IniciarEvaluacionDTO
                                    {
                                        EvaluacionId = evaluacionId,
                                        FechaInicio = sesionExistente.FechaInicio,
                                        TiempoLimiteMins = evaluacion.TiempoLimiteMins,
                                        NumeroIntento = sesionExistente.NumeroIntento,
                                        Token = sesionExistente.Token
                                    };

                                    await transaction.CommitAsync();
                                    return ResultadoOperacion<IniciarEvaluacionDTO>.Exito(iniciarDtoExistente);
                                }
                            }

                            var numeroIntento = intentosRealizados + 1;
                            _logger.LogInformation($"Creando nueva sesión para usuario {usuarioId}, evaluación {evaluacionId}, intento número {numeroIntento}");

                            var nuevaSesion = new SesionEvaluacion
                            {
                                UsuarioId = usuarioId,
                                EvaluacionId = evaluacionId,
                                NumeroIntento = numeroIntento,
                                FechaInicio = DateTime.UtcNow
                            };

                            _context.SesionesEvaluacion.Add(nuevaSesion);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation($"Sesión creada exitosamente con ID {nuevaSesion.Id}, Token: {nuevaSesion.Token}");

                            var iniciarDto = new IniciarEvaluacionDTO
                            {
                                EvaluacionId = evaluacionId,
                                FechaInicio = nuevaSesion.FechaInicio,
                                TiempoLimiteMins = evaluacion.TiempoLimiteMins,
                                NumeroIntento = numeroIntento,
                                Token = nuevaSesion.Token
                            };

                            await transaction.CommitAsync();
                            _logger.LogInformation($"Evaluación iniciada exitosamente para usuario {usuarioId}");
                            return ResultadoOperacion<IniciarEvaluacionDTO>.Exito(iniciarDto);
                        }
                        catch (Exception ex)
                        {
                            // Check for serialization failure and retry a few times
                            var message = ex.InnerException?.Message ?? ex.Message;
                            _logger.LogWarning(ex, $"Error dentro de la transacción al iniciar evaluación (intento {attempt + 1}/{maxRetries}): {message}");

                            try
                            {
                                await transaction.RollbackAsync();
                            }
                            catch { }

                            // Common PostgreSQL serialization failure contains 'could not serialize' or SQLSTATE 40001
                            if (message != null && (message.Contains("could not serialize") || message.Contains("40001")))
                            {
                                // brief backoff
                                await Task.Delay(50 * (attempt + 1));
                                continue; // retry
                            }

                            // other errors -> bubble up
                            throw;
                        }
                    }
                }

                // If all retries exhausted
                _logger.LogError($"No se pudo iniciar la evaluación después de {maxRetries} intentos debido a conflictos de concurrencia");
                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Error al iniciar evaluación por conflicto concurrente, intente nuevamente", 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar evaluación");
                return ResultadoOperacion<IniciarEvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<EvaluacionParaRealizarDTO>> ObtenerEvaluacionParaRealizarAsync(int evaluacionId, string token)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioId))
                {
                    return ResultadoOperacion<EvaluacionParaRealizarDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var sesion = await _context.SesionesEvaluacion
                    .Include(s => s.Evaluacion)
                        .ThenInclude(e => e.Preguntas)
                            .ThenInclude(p => p.Opciones)
                    .FirstOrDefaultAsync(s => s.EvaluacionId == evaluacionId && 
                                            s.UsuarioId == usuarioId && 
                                            s.Token == token && 
                                            s.Activa);

                if (sesion == null)
                {
                    return ResultadoOperacion<EvaluacionParaRealizarDTO>.Fallo(mensaje: "Sesión de evaluación no válida", 400);
                }

                if (sesion.EstaVencida)
                {
                    sesion.Activa = false;
                    sesion.TiempoAgotado = true;
                    sesion.FechaFinalizacion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<EvaluacionParaRealizarDTO>.Fallo(mensaje: "El tiempo de la evaluación ha expirado", 400);
                }

                var evaluacionDto = new EvaluacionParaRealizarDTO
                {
                    Id = sesion.Evaluacion.Id,
                    Titulo = sesion.Evaluacion.Titulo,
                    Descripcion = sesion.Evaluacion.Descripcion,
                    TiempoLimiteMins = sesion.Evaluacion.TiempoLimiteMins,
                    NumeroIntento = sesion.NumeroIntento,
                    FechaInicio = sesion.FechaInicio,
                    Preguntas = sesion.Evaluacion.Preguntas.Select(p => new PreguntaEstudianteDTO
                    {
                        Id = p.Id,
                        Texto = p.Texto,
                        Orden = p.Orden,
                        Puntos = p.Puntos,
                        Opciones = p.Opciones.Select(o => new OpcionEstudianteDTO
                        {
                            Id = o.Id,
                            Texto = o.Texto,
                            Orden = o.Orden
                        }).OrderBy(o => o.Orden).ToList()
                    }).OrderBy(p => p.Orden).ToList()
                };

                return ResultadoOperacion<EvaluacionParaRealizarDTO>.Exito(evaluacionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener evaluación para realizar");
                return ResultadoOperacion<EvaluacionParaRealizarDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<bool>> AbandonarSesionAsync(int evaluacionId, string token)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioId))
                {
                    return ResultadoOperacion<bool>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var sesion = await _context.SesionesEvaluacion
                    .FirstOrDefaultAsync(s => s.EvaluacionId == evaluacionId && s.UsuarioId == usuarioId && s.Token == token && s.Activa);

                if (sesion == null)
                {
                    return ResultadoOperacion<bool>.Fallo(mensaje: "Sesión no encontrada o no activa", 404);
                }

                sesion.Activa = false;
                // marcar fecha de finalización; no existe campo 'Abandonada' en el modelo actual
                sesion.FechaFinalizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.Exito(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar sesión como abandonada");
                return ResultadoOperacion<bool>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<HistorialEvaluacionDTO>>> ObtenerHistorialEvaluacionesAsync()
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioId))
                {
                    return ResultadoOperacion<List<HistorialEvaluacionDTO>>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var historial = await _context.RespuestasUsuario
                    .Include(r => r.Evaluacion)
                        .ThenInclude(e => e.Curso)
                    .Include(r => r.Pregunta)
                    .Include(r => r.Opcion)
                    .Where(r => r.UsuarioId == usuarioId)
                    .GroupBy(r => new { r.EvaluacionId, r.NumeroIntento })
                    .Select(g => new {
                        EvaluacionId = g.Key.EvaluacionId,
                        NumeroIntento = g.Key.NumeroIntento,
                        Evaluacion = g.First().Evaluacion,
                        FechaRealizada = g.Max(r => r.FechaRespuesta),
                        PuntajeObtenido = g.Sum(r => r.Opcion.EsCorrecta ? r.Pregunta.Puntos : 0),
                        PuntajeMaximo = g.Sum(r => r.Pregunta.Puntos)
                    })
                    .OrderByDescending(h => h.FechaRealizada)
                    .ToListAsync();

                var historialDto = historial.Select(h => new HistorialEvaluacionDTO
                {
                    EvaluacionId = h.EvaluacionId,
                    Titulo = h.Evaluacion.Titulo,
                    CursoNombre = h.Evaluacion.Curso.Nombre,
                    NumeroIntento = h.NumeroIntento,
                    FechaRealizada = h.FechaRealizada,
                    PuntajeObtenido = h.PuntajeObtenido,
                    PuntajeMaximo = h.PuntajeMaximo,
                    Porcentaje = Math.Round((double)h.PuntajeObtenido / h.PuntajeMaximo * 100, 2),
                    Completada = true,
                    Estado = "Completada"
                }).ToList();

                return ResultadoOperacion<List<HistorialEvaluacionDTO>>.Exito(historialDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de evaluaciones");
                return ResultadoOperacion<List<HistorialEvaluacionDTO>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<ResultadoEvaluacionDTO>> SubmitRespuestasConTokenAsync(SubmitRespuestasDTO dto, string token)
        {
            try
            {
                var contexto = _httpContextAccessor.HttpContext;
                var usuarioId = contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(usuarioId))
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
                }

                var sesion = await _context.SesionesEvaluacion
                    .Include(s => s.Evaluacion)
                        .ThenInclude(e => e.Preguntas)
                            .ThenInclude(p => p.Opciones)
                    .FirstOrDefaultAsync(s => s.EvaluacionId == dto.EvaluacionId && 
                                            s.UsuarioId == usuarioId && 
                                            s.Token == token && 
                                            s.Activa);

                if (sesion == null)
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Sesión inválida - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Token: {token}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Sesión de evaluación no válida", 400);
                }

                // Validación anti-trampa: Verificar tiempo de sesión
                var ahora = DateTime.UtcNow;
                var tiempoTranscurrido = ahora - sesion.FechaInicio;
                var tiempoLimiteMinutos = sesion.Evaluacion?.TiempoLimiteMins ?? 60;
                var tiempoLimite = TimeSpan.FromMinutes(tiempoLimiteMinutos);

                if (tiempoTranscurrido > tiempoLimite.Add(TimeSpan.FromMinutes(5))) // 5 minutos de tolerancia
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Tiempo excedido - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Transcurrido: {tiempoTranscurrido.TotalMinutes:F2}min, Límite: {tiempoLimiteMinutos}min");
                    sesion.Activa = false;
                    sesion.TiempoAgotado = true;
                    await _context.SaveChangesAsync();
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "El tiempo de la evaluación ha expirado", 400);
                }

                // Validación anti-trampa: Verificar tiempo mínimo por pregunta (al menos 5 segundos por pregunta)
                var tiempoMinimoPorPregunta = TimeSpan.FromSeconds(5);
                var numeroPreguntas = dto.Respuestas.Count;
                var tiempoMinimoTotal = TimeSpan.FromTicks(tiempoMinimoPorPregunta.Ticks * numeroPreguntas);

                if (tiempoTranscurrido < tiempoMinimoTotal)
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Respuestas demasiado rápidas - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Transcurrido: {tiempoTranscurrido.TotalSeconds:F1}s, Mínimo esperado: {tiempoMinimoTotal.TotalSeconds:F1}s");
                    // No bloqueamos, pero registramos la actividad sospechosa
                }

                // Validación anti-trampa: Verificar integridad de respuestas
                var preguntasEvaluacion = sesion.Evaluacion?.Preguntas.ToList() ?? new List<Pregunta>();
                var preguntasIdsValidas = preguntasEvaluacion.Select(p => p.Id).ToHashSet();

                foreach (var respuesta in dto.Respuestas)
                {
                    if (!preguntasIdsValidas.Contains(respuesta.PreguntaId))
                    {
                        _logger.LogWarning($"[ANTI-TRAMPA] Pregunta inválida - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, PreguntaId: {respuesta.PreguntaId}");
                        return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                    }

                    var pregunta = preguntasEvaluacion.First(p => p.Id == respuesta.PreguntaId);
                    var opcionesIdsValidas = pregunta.Opciones.Select(o => o.Id).ToHashSet();

                    if (!opcionesIdsValidas.Contains(respuesta.OpcionId))
                    {
                        _logger.LogWarning($"[ANTI-TRAMPA] Opción inválida - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, PreguntaId: {respuesta.PreguntaId}, OpcionId: {respuesta.OpcionId}");
                        return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                    }
                }

                // Validación anti-trampa: Verificar que no haya respuestas duplicadas para la misma pregunta
                var preguntasRespondidas = dto.Respuestas.Select(r => r.PreguntaId).ToList();
                if (preguntasRespondidas.Count != preguntasRespondidas.Distinct().Count())
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Respuestas duplicadas - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Datos de evaluación inválidos", 400);
                }

                // Validación anti-trampa: Verificar que se respondieron todas las preguntas
                if (preguntasRespondidas.Count != preguntasEvaluacion.Count)
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] No todas las preguntas respondidas - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Respondidas: {preguntasRespondidas.Count}, Total: {preguntasEvaluacion.Count}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Debe responder todas las preguntas", 400);
                }

                if (sesion.EstaVencida)
                {
                    _logger.LogWarning($"[ANTI-TRAMPA] Sesión vencida al enviar - Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}");
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "El tiempo de la evaluación ha expirado", 400);
                }

                var respuestasExistentes = await _context.RespuestasUsuario
                    .Where(r => r.EvaluacionId == dto.EvaluacionId && 
                               r.UsuarioId == usuarioId && 
                               r.NumeroIntento == sesion.NumeroIntento)
                    .ToListAsync();

                _context.RespuestasUsuario.RemoveRange(respuestasExistentes);

                var respuestasUsuario = dto.Respuestas.Select(r => new RespuestaUsuario
                {
                    UsuarioId = usuarioId,
                    EvaluacionId = dto.EvaluacionId,
                    PreguntaId = r.PreguntaId,
                    OpcionId = r.OpcionId,
                    NumeroIntento = sesion.NumeroIntento
                }).ToList();

                _context.RespuestasUsuario.AddRange(respuestasUsuario);

                sesion.Activa = false;
                sesion.Completada = true;
                sesion.FechaFinalizacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[EVALUACIÓN COMPLETADA] Usuario: {usuarioId}, Evaluación: {dto.EvaluacionId}, Intento: {sesion.NumeroIntento}, Tiempo: {tiempoTranscurrido.TotalMinutes:F2}min");

                return await ObtenerResultadoEvaluacionAsync(dto.EvaluacionId, usuarioId, sesion.NumeroIntento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar respuestas con token");
                return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<ResultadoEvaluacionDTO>> ResponderEvaluacionConTokenAsync(
            int evaluacionId,
            string token,
            SubmitRespuestasDTO dto)
        {
            try
            {
                // Validar que el token corresponda a una sesión activa
                var sesion = await _context.SesionesEvaluacion
                    .Include(s => s.Evaluacion)
                    .Include(s => s.Usuario)
                    .FirstOrDefaultAsync(s => s.Token == token && s.Activa == true);

                if (sesion == null)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Sesión de evaluación no encontrada o expirada", 404);
                }

                if (sesion.EvaluacionId != evaluacionId)
                {
                    return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "El token no corresponde a esta evaluación", 400);
                }

                // Validar que las respuestas pertenezcan a esta evaluación
                foreach (var respuesta in dto.Respuestas)
                {
                    if (respuesta.EvaluacionId != evaluacionId)
                    {
                        return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Una o más respuestas no pertenecen a esta evaluación", 400);
                    }
                }

                // Procesar las respuestas usando el método existente
                var resultado = await SubmitRespuestasConTokenAsync(dto, token);
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al responder evaluación con token");
                return ResultadoOperacion<ResultadoEvaluacionDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<ResultadoEstudianteEvaluacionDTO>>> ObtenerResultadosEstudiantesEvaluacionAsync(int evaluacionId)
        {
            try
            {
                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Curso)
                    .FirstOrDefaultAsync(e => e.Id == evaluacionId);

                if (evaluacion == null)
                {
                    return ResultadoOperacion<List<ResultadoEstudianteEvaluacionDTO>>.Fallo("Evaluación no encontrada", 404);
                }

                // Obtener todos los estudiantes inscritos en el curso
                var estudiantesCurso = await _context.CursoUsuarios
                    .Where(cu => cu.CursoId == evaluacion.CursoId && cu.RolEnCurso == RolEnCurso.Alumno)
                    .Select(cu => cu.UsuarioId)
                    .ToListAsync();

                var resultados = new List<ResultadoEstudianteEvaluacionDTO>();

                foreach (var estudianteId in estudiantesCurso)
                {
                    var estudiante = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == estudianteId);

                    if (estudiante == null) continue;

                    // Obtener la mejor nota del estudiante para esta evaluación
                    var mejorNota = await _context.Notas
                        .Where(n => n.EvaluacionId == evaluacionId && n.UsuarioId == estudianteId)
                        .OrderByDescending(n => n.Calificacion)
                        .FirstOrDefaultAsync();

                    // Obtener información de intentos
                    var sesionesEvaluacion = await _context.SesionesEvaluacion
                        .Where(s => s.EvaluacionId == evaluacionId && s.UsuarioId == estudianteId)
                        .ToListAsync();

                    var intentos = sesionesEvaluacion.Count;
                    var completada = sesionesEvaluacion.Any(s => s.Completada);
                    var fechaRealizacion = sesionesEvaluacion
                        .Where(s => s.Completada)
                        .OrderByDescending(s => s.FechaFinalizacion)
                        .Select(s => s.FechaFinalizacion)
                        .FirstOrDefault();

                    // Calcular porcentaje si hay nota
                    double? porcentaje = null;
                    if (mejorNota != null)
                    {
                        // El porcentaje se calcula basado en la escala chilena (1.0-7.0)
                        // 7.0 = 100%, 1.0 = 0%, pero en realidad el mínimo aprobado es 4.0
                        porcentaje = ((double)((mejorNota.Calificacion - 1.0m) / (7.0m - 1.0m))) * 100.0;
                        if (porcentaje < 0) porcentaje = 0;
                        if (porcentaje > 100) porcentaje = 100;
                    }

                    var resultadoEstudiante = new ResultadoEstudianteEvaluacionDTO
                    {
                        UsuarioId = estudiante.Id,
                        UsuarioRut = estudiante.Rut,
                        NombreCompleto = estudiante.Nombre ?? "N/A",
                        Correo = estudiante.Email ?? "",
                        Nota = mejorNota?.Calificacion,
                        Porcentaje = porcentaje,
                        FechaRealizacion = fechaRealizacion,
                        Intentos = intentos,
                        Completada = completada
                    };

                    resultados.Add(resultadoEstudiante);
                }

                // Ordenar por nombre completo
                resultados = resultados
                    .OrderBy(r => r.NombreCompleto)
                    .ToList();

                return ResultadoOperacion<List<ResultadoEstudianteEvaluacionDTO>>.Exito(resultados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resultados de estudiantes para evaluación {EvaluacionId}", evaluacionId);
                return ResultadoOperacion<List<ResultadoEstudianteEvaluacionDTO>>.Fallo("Error interno del servidor", 500);
            }
        }
    }
}