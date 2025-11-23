using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.DTOs;
using System.Security.Claims;
using OfficeOpenXml;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // HU-01: Consulta de notas del alumno
        [HttpGet("mis-notas")]
        public async Task<ActionResult<IEnumerable<NotasEstudianteDTO>>> GetMisNotas()
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized("Usuario no identificado");
            }

            // Obtener todas las notas del estudiante (no solo finales)
            var todasLasNotas = await _context.Notas
                .Include(n => n.Evaluacion)
                    .ThenInclude(e => e.Curso)
                        .ThenInclude(c => c.Administrador)
                .Include(n => n.Evaluacion)
                    .ThenInclude(e => e.Docente)
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.FechaCalificacion)
                .ToListAsync();

            // Agrupar por evaluación
            var notasPorEvaluacion = todasLasNotas
                .GroupBy(n => n.EvaluacionId)
                .Select(grupo => {
                    var evaluacion = grupo.First().Evaluacion;
                    var notas = grupo.Select(n => new NotaDTO
                    {
                        Id = n.Id,
                        Calificacion = n.Calificacion,
                        UsuarioId = n.UsuarioId,
                        EvaluacionId = n.EvaluacionId,
                        EvaluacionTitulo = n.Evaluacion.Titulo,
                        FechaCalificacion = n.FechaCalificacion,
                        Observaciones = n.Observaciones,
                        NumeroIntento = n.NumeroIntento,
                        EsNotaFinal = n.EsNotaFinal
                    }).ToList();

                    var mejorNota = notas.Max(n => n.Calificacion);
                    var notaFinal = notas.FirstOrDefault(n => n.EsNotaFinal)?.Calificacion;

                    return new NotasEstudianteDTO
                    {
                        EvaluacionId = evaluacion.Id,
                        EvaluacionTitulo = evaluacion.Titulo,
                        CursoNombre = evaluacion.Curso?.Nombre ?? "Curso no encontrado",
                        DocenteNombre = evaluacion.Docente?.Nombre ?? "Docente no encontrado",
                        Notas = notas,
                        MejorNota = mejorNota,
                        NotaFinal = notaFinal,
                        EsLaboratorio3DLab = evaluacion.EsLaboratorio3DLab
                    };
                })
                .OrderByDescending(n => n.NotaFinal ?? n.MejorNota)
                .ToList();

            return Ok(notasPorEvaluacion);
        }

        // HU-01: Consulta de nota específica por evaluación
        [HttpGet("evaluacion/{evaluacionId}")]
        public async Task<ActionResult<NotaDTO>> GetNotaPorEvaluacion(int evaluacionId)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized("Usuario no identificado");
            }

            var nota = await _context.Notas
                .Include(n => n.Evaluacion)
                .Where(n => n.UsuarioId == usuarioId && n.EvaluacionId == evaluacionId && n.EsNotaFinal)
                .Select(n => new NotaDTO
                {
                    Id = n.Id,
                    Calificacion = n.Calificacion,
                    UsuarioId = n.UsuarioId,
                    EvaluacionId = n.EvaluacionId,
                    EvaluacionTitulo = n.Evaluacion.Titulo,
                    FechaCalificacion = n.FechaCalificacion,
                    Observaciones = n.Observaciones,
                    NumeroIntento = n.NumeroIntento,
                    EsNotaFinal = n.EsNotaFinal
                })
                .FirstOrDefaultAsync();

            if (nota == null)
            {
                return NotFound("Nota no encontrada para esta evaluación");
            }

            return Ok(nota);
        }

        // HU-03: Consulta de notas de alumnos por el docente
        [HttpGet("curso/{cursoId}/estudiantes")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<IEnumerable<NotaDTO>>> GetNotasEstudiantesPorCurso(int cursoId)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Verificar que el docente tenga acceso al curso
            if (userRole == "Docente")
            {
                var tieneAcceso = await _context.Evaluaciones
                    .AnyAsync(e => e.CursoId == cursoId && e.DocenteId == usuarioId);

                if (!tieneAcceso)
                {
                    return Forbid("No tiene permisos para ver las notas de este curso");
                }
            }

            var notas = await _context.Notas
                .Include(n => n.Usuario)
                .Include(n => n.Evaluacion)
                .Where(n => n.Evaluacion.CursoId == cursoId && n.EsNotaFinal)
                .OrderBy(n => n.Usuario.Nombre)
                .ThenByDescending(n => n.FechaCalificacion)
                .Select(n => new NotaDTO
                {
                    Id = n.Id,
                    Calificacion = n.Calificacion,
                    UsuarioId = n.UsuarioId,
                    UsuarioNombre = n.Usuario.Nombre,
                    UsuarioRut = n.Usuario.Rut,
                    EvaluacionId = n.EvaluacionId,
                    EvaluacionTitulo = n.Evaluacion.Titulo,
                    FechaCalificacion = n.FechaCalificacion,
                    Observaciones = n.Observaciones,
                    NumeroIntento = n.NumeroIntento,
                    EsNotaFinal = n.EsNotaFinal
                })
                .ToListAsync();

            return Ok(notas);
        }

        // HU-03: Consulta de notas de un estudiante específico
        [HttpGet("estudiante/{estudianteId}")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<IEnumerable<NotaDTO>>> GetNotasEstudiante(string estudianteId)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Verificar que el usuario existe y es estudiante
            var estudiante = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == estudianteId && u.Rol == "Estudiante");

            if (estudiante == null)
            {
                return NotFound("Estudiante no encontrado");
            }

            var query = _context.Notas
                .Include(n => n.Usuario)
                .Include(n => n.Evaluacion)
                    .ThenInclude(e => e.Curso)
                .Where(n => n.UsuarioId == estudianteId && n.EsNotaFinal);

            // Si es docente, filtrar solo sus cursos
            if (userRole == "Docente")
            {
                query = query.Where(n => n.Evaluacion.DocenteId == usuarioId);
            }

            var notas = await query
                .OrderByDescending(n => n.FechaCalificacion)
                .Select(n => new NotaDTO
                {
                    Id = n.Id,
                    Calificacion = n.Calificacion,
                    UsuarioId = n.UsuarioId,
                    UsuarioNombre = n.Usuario.Nombre,
                    UsuarioRut = n.Usuario.Rut,
                    EvaluacionId = n.EvaluacionId,
                    EvaluacionTitulo = n.Evaluacion.Titulo,
                    FechaCalificacion = n.FechaCalificacion,
                    Observaciones = n.Observaciones,
                    NumeroIntento = n.NumeroIntento,
                    EsNotaFinal = n.EsNotaFinal
                })
                .ToListAsync();

            return Ok(notas);
        }

        // Crear/Actualizar nota (para docentes)
        [HttpPost]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<NotaDTO>> CrearNota(NotaCreacionDTO notaDto)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Verificar que la evaluación existe
            var evaluacion = await _context.Evaluaciones
                .FirstOrDefaultAsync(e => e.Id == notaDto.EvaluacionId);

            if (evaluacion == null)
            {
                return NotFound("Evaluación no encontrada");
            }

            // Verificar que el docente tiene permisos sobre la evaluación
            if (userRole == "Docente" && evaluacion.DocenteId != usuarioId)
            {
                return Forbid("No tiene permisos para calificar esta evaluación");
            }

            // Verificar que el estudiante existe
            var estudiante = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == notaDto.UsuarioId && u.Rol == "Estudiante");

            if (estudiante == null)
            {
                return NotFound("Estudiante no encontrado");
            }

            // Verificar si ya existe una nota para este intento
            var notaExistente = await _context.Notas
                .FirstOrDefaultAsync(n => n.UsuarioId == notaDto.UsuarioId &&
                                        n.EvaluacionId == notaDto.EvaluacionId &&
                                        n.NumeroIntento == notaDto.NumeroIntento);

            if (notaExistente != null)
            {
                // Actualizar nota existente
                notaExistente.Calificacion = notaDto.Calificacion;
                notaExistente.Observaciones = notaDto.Observaciones;
                notaExistente.EsNotaFinal = notaDto.EsNotaFinal;
                notaExistente.FechaCalificacion = DateTime.UtcNow;
            }
            else
            {
                // Crear nueva nota
                var nuevaNota = new Nota
                {
                    Calificacion = notaDto.Calificacion,
                    UsuarioId = notaDto.UsuarioId,
                    EvaluacionId = notaDto.EvaluacionId,
                    Observaciones = notaDto.Observaciones,
                    NumeroIntento = notaDto.NumeroIntento,
                    EsNotaFinal = notaDto.EsNotaFinal,
                    FechaCalificacion = DateTime.UtcNow
                };

                _context.Notas.Add(nuevaNota);
            }

            await _context.SaveChangesAsync();

            var notaResultado = await _context.Notas
                .Include(n => n.Usuario)
                .Include(n => n.Evaluacion)
                .Where(n => n.UsuarioId == notaDto.UsuarioId &&
                          n.EvaluacionId == notaDto.EvaluacionId &&
                          n.NumeroIntento == notaDto.NumeroIntento)
                .Select(n => new NotaDTO
                {
                    Id = n.Id,
                    Calificacion = n.Calificacion,
                    UsuarioId = n.UsuarioId,
                    UsuarioNombre = n.Usuario.Nombre,
                    EvaluacionId = n.EvaluacionId,
                    EvaluacionTitulo = n.Evaluacion.Titulo,
                    FechaCalificacion = n.FechaCalificacion,
                    Observaciones = n.Observaciones,
                    NumeroIntento = n.NumeroIntento,
                    EsNotaFinal = n.EsNotaFinal
                })
                .FirstOrDefaultAsync();

            return Ok(notaResultado);
        }

        // HU-04: Exportar notas de estudiantes por curso a Excel
        [HttpGet("curso/{cursoId}/exportar-excel")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> ExportarNotasCursoExcel(int cursoId)
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Verificar que el docente tenga acceso al curso
            if (userRole == "Docente")
            {
                var tieneAcceso = await _context.Evaluaciones
                    .AnyAsync(e => e.CursoId == cursoId && e.DocenteId == usuarioId);

                if (!tieneAcceso)
                {
                    return Forbid("No tiene permisos para ver las notas de este curso");
                }
            }

            // Obtener información del curso
            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.Nrc == cursoId);

            if (curso == null)
            {
                return NotFound("Curso no encontrado");
            }

            // Obtener todas las notas del curso
            var notas = await _context.Notas
                .Include(n => n.Usuario)
                .Include(n => n.Evaluacion)
                    .ThenInclude(e => e.Curso)
                .Where(n => n.Evaluacion.CursoId == cursoId && n.EsNotaFinal)
                .OrderBy(n => n.Usuario.Nombre)
                .ThenBy(n => n.Evaluacion.Titulo)
                .ToListAsync();

            // Configurar EPPlus (mantener compatibilidad con versiones previas)
#pragma warning disable CS0618
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Notas del Curso");

                // Encabezados
                worksheet.Cells[1, 1].Value = "Curso";
                worksheet.Cells[1, 2].Value = curso.Nombre;
                worksheet.Cells[2, 1].Value = "Fecha de Exportación";
                worksheet.Cells[2, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // Encabezados de tabla
                worksheet.Cells[4, 1].Value = "RUT";
                worksheet.Cells[4, 2].Value = "Nombre del Estudiante";
                worksheet.Cells[4, 3].Value = "Evaluación";
                worksheet.Cells[4, 4].Value = "Calificación";
                worksheet.Cells[4, 5].Value = "Fecha de Calificación";
                worksheet.Cells[4, 6].Value = "Observaciones";

                // Estilo de encabezados
                using (var range = worksheet.Cells[4, 1, 4, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Datos
                int row = 5;
                foreach (var nota in notas)
                {
                    worksheet.Cells[row, 1].Value = nota.Usuario?.Rut ?? "N/A";
                    worksheet.Cells[row, 2].Value = nota.Usuario?.Nombre ?? "N/A";
                    worksheet.Cells[row, 3].Value = nota.Evaluacion?.Titulo ?? "N/A";
                    worksheet.Cells[row, 4].Value = nota.Calificacion;
                    worksheet.Cells[row, 5].Value = nota.FechaCalificacion.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 6].Value = nota.Observaciones ?? "";
                    row++;
                }

                // Auto ajustar columnas
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Convertir a bytes
                var excelBytes = package.GetAsByteArray();

                // Nombre del archivo
                var fileName = $"Notas_{curso.Nombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}