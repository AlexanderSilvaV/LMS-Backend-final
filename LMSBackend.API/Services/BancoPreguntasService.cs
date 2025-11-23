using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace LMSBackend.API.Services
{
    public class BancoPreguntasService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BancoPreguntasService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BancoPreguntasService(ApplicationDbContext context, ILogger<BancoPreguntasService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private string? ObtenerUsuarioId()
        {
            var contexto = _httpContextAccessor.HttpContext;
            return contexto?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<ResultadoOperacion<BancoPreguntaDTO>> AgregarPreguntaAsync(BancoPreguntaCreacionDTO dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            if (dto.Opciones == null || dto.Opciones.Count < 2)
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "La pregunta debe tener al menos 2 opciones", 400);
            }

            if (!dto.Opciones.Any(o => o.EsCorrecta))
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "La pregunta debe tener al menos una respuesta correcta", 400);
            }

            // Verificar duplicados por enunciado y categoría para el mismo docente
            var existePregunta = await _context.BancoPreguntas
                .AnyAsync(bp => bp.Texto == dto.Enunciado
                          && bp.Categoria == dto.Categoria
                          && bp.DocenteId == usuarioId);

            if (existePregunta)
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Ya existe una pregunta con el mismo enunciado en esta categoría", 400);
            }

            try
            {
                // Auto-crear categoría si no existe
                var categoriaFinal = await CrearOObtenerCategoriaAsync(dto.Categoria, usuarioId);

                var bancoPregunta = new BancoPregunta
                {
                    Texto = dto.Enunciado,
                    Categoria = categoriaFinal,
                    DocenteId = usuarioId,
                    Puntos = dto.Puntos,
                    Activa = dto.Activa,
                    Dificultad = 1, // Valor por defecto
                    TextoNormalizado = dto.Enunciado.ToLowerInvariant(),
                    FechaCreacionUtc = DateTime.UtcNow,
                    AutorId = usuarioId
                };

                _context.BancoPreguntas.Add(bancoPregunta);
                await _context.SaveChangesAsync();

                foreach (var opcionDto in dto.Opciones)
                {
                    var opcion = new OpcionBanco
                    {
                        Texto = opcionDto.Texto,
                        EsCorrecta = opcionDto.EsCorrecta,
                        Orden = opcionDto.Orden,
                        BancoPreguntaId = bancoPregunta.Id
                    };

                    _context.OpcionesBanco.Add(opcion);
                }

                await _context.SaveChangesAsync();

                var preguntaDto = await MapearABancoPreguntaDTO(bancoPregunta.Id);
                return ResultadoOperacion<BancoPreguntaDTO>.Exito(preguntaDto, "Pregunta agregada exitosamente", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar pregunta al banco");
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<BancoPreguntaDTO>> ActualizarPreguntaAsync(int id, BancoPreguntaEdicionDTO dto)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            var pregunta = await _context.BancoPreguntas
                .Include(bp => bp.Opciones)
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (pregunta == null)
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Pregunta no encontrada", 404);
            }

            if (pregunta.DocenteId != usuarioId)
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "No autorizado", 403);
            }

            if (dto.Opciones == null || dto.Opciones.Count < 2)
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "La pregunta debe tener al menos 2 opciones", 400);
            }

            if (!dto.Opciones.Any(o => o.EsCorrecta))
            {
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "La pregunta debe tener al menos una respuesta correcta", 400);
            }

            try
            {
                pregunta.Texto = dto.Enunciado;
                pregunta.Categoria = dto.Categoria;
                pregunta.Puntos = dto.Puntos;
                pregunta.Activa = dto.Activa;
                pregunta.FechaModificacion = DateTime.UtcNow;

                // Eliminar opciones existentes
                _context.OpcionesBanco.RemoveRange(pregunta.Opciones);

                // Agregar nuevas opciones
                foreach (var opcionDto in dto.Opciones)
                {
                    var opcion = new OpcionBanco
                    {
                        Texto = opcionDto.Texto,
                        EsCorrecta = opcionDto.EsCorrecta,
                        Orden = opcionDto.Orden,
                        BancoPreguntaId = pregunta.Id
                    };

                    _context.OpcionesBanco.Add(opcion);
                }

                await _context.SaveChangesAsync();

                var preguntaDto = await MapearABancoPreguntaDTO(pregunta.Id);
                return ResultadoOperacion<BancoPreguntaDTO>.Exito(preguntaDto, "Pregunta actualizada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar pregunta del banco");
                return ResultadoOperacion<BancoPreguntaDTO>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<bool>> EliminarPreguntaAsync(int id)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<bool>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            var pregunta = await _context.BancoPreguntas
                .FirstOrDefaultAsync(bp => bp.Id == id);

            if (pregunta == null)
            {
                return ResultadoOperacion<bool>.Fallo(mensaje: "Pregunta no encontrada", 404);
            }

            if (pregunta.DocenteId != usuarioId)
            {
                return ResultadoOperacion<bool>.Fallo(mensaje: "No autorizado", 403);
            }

            try
            {
                _context.BancoPreguntas.Remove(pregunta);
                await _context.SaveChangesAsync();

                return ResultadoOperacion<bool>.Exito(true, "Pregunta eliminada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar pregunta del banco");
                return ResultadoOperacion<bool>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<Page<BancoPreguntaDTO>>> ListarPreguntasAsync(PaginacionDTO paginacion, string? categoria = null)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<Page<BancoPreguntaDTO>>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            try
            {
                var query = _context.BancoPreguntas
                    .Include(bp => bp.Opciones)
                    .Where(bp => bp.DocenteId == usuarioId);

                if (!string.IsNullOrEmpty(categoria))
                {
                    query = query.Where(bp => bp.Categoria == categoria);
                }

                var totalItems = await query.CountAsync();
                var preguntas = await query
                    .OrderByDescending(bp => bp.FechaCreacion)
                    .Skip((paginacion.PaginaActual - 1) * paginacion.CantidadPorPagina)
                    .Take(paginacion.CantidadPorPagina)
                    .ToListAsync();

                var preguntasDto = preguntas.Select(p => MapearABancoPreguntaDTO(p)).ToList();

                var page = new Page<BancoPreguntaDTO>
                {
                    Items = preguntasDto,
                    TotalItems = totalItems,
                    PageNumber = paginacion.PaginaActual,
                    PageSize = paginacion.CantidadPorPagina,
                    TotalPages = (int)Math.Ceiling((double)totalItems / paginacion.CantidadPorPagina)
                };

                return ResultadoOperacion<Page<BancoPreguntaDTO>>.Exito(page, "Preguntas obtenidas exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar preguntas del banco");
                return ResultadoOperacion<Page<BancoPreguntaDTO>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<List<string>>> ObtenerCategoriasAsync()
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<List<string>>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            try
            {
                // Fallback temporal: obtener categorías de preguntas existentes
                var categorias = await _context.BancoPreguntas
                    .Where(bp => bp.DocenteId == usuarioId && !string.IsNullOrEmpty(bp.Categoria))
                    .Select(bp => bp.Categoria!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return ResultadoOperacion<List<string>>.Exito(categorias, "Categorías obtenidas exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías");
                return ResultadoOperacion<List<string>>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        public async Task<ResultadoOperacion<string>> AgregarCategoriaAsync(string categoria)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<string>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            if (string.IsNullOrWhiteSpace(categoria))
            {
                return ResultadoOperacion<string>.Fallo(mensaje: "La categoría no puede estar vacía", 400);
            }

            if (categoria.Length > 100)
            {
                return ResultadoOperacion<string>.Fallo(mensaje: "La categoría no puede tener más de 100 caracteres", 400);
            }

            try
            {
                var categoriaLimpia = categoria.Trim();

                // Verificar si ya existe esta categoría en las preguntas del usuario
                var existeCategoria = await _context.BancoPreguntas
                    .AnyAsync(bp => bp.DocenteId == usuarioId && bp.Categoria == categoriaLimpia);

                if (existeCategoria)
                {
                    return ResultadoOperacion<string>.Fallo(mensaje: "Ya existe esta categoría", 400);
                }

                // Por ahora, simplemente validamos que la categoría es válida
                return ResultadoOperacion<string>.Exito(categoriaLimpia, "Categoría válida para usar");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar categoría");
                return ResultadoOperacion<string>.Fallo(mensaje: "Error interno del servidor", 500);
            }
        }

        private Task<string?> CrearOObtenerCategoriaAsync(string? nombreCategoria, string usuarioId)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria))
                return Task.FromResult<string?>(null);

            var categoriaLimpia = nombreCategoria.Trim();

            // Por ahora, simplemente devolver la categoría limpia
            // En el futuro, aquí se creará en la tabla de categorías
            return Task.FromResult<string?>(categoriaLimpia);
        }

        private async Task<BancoPreguntaDTO> MapearABancoPreguntaDTO(int id)
        {
            var pregunta = await _context.BancoPreguntas
                .Include(bp => bp.Opciones)
                .FirstAsync(bp => bp.Id == id);

            return MapearABancoPreguntaDTO(pregunta);
        }

        public async Task<ResultadoOperacion<ImportacionExcelDTO>> ImportarDesdeExcelAsync(IFormFile archivo)
        {
            var usuarioId = ObtenerUsuarioId();
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<ImportacionExcelDTO>.Fallo(mensaje: "Usuario no autenticado", 401);
            }

            if (archivo == null || archivo.Length == 0)
            {
                return ResultadoOperacion<ImportacionExcelDTO>.Fallo(mensaje: "No se proporcionó ningún archivo", 400);
            }

            var extensionesPermitidas = new[] { ".xlsx", ".xls" };
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
            {
                return ResultadoOperacion<ImportacionExcelDTO>.Fallo(mensaje: "Solo se permiten archivos Excel (.xlsx, .xls)", 400);
            }

            var importacionDto = new ImportacionExcelDTO
            {
                FileName = archivo.FileName,
                CantidadPreguntasImportadas = 0,
                CantidadPreguntasConErrores = 0,
                Errores = new List<string>(),
                Estado = "En progreso"
            };

            try
            {
                using var stream = archivo.OpenReadStream();
                XLWorkbook workbook;
                IXLWorksheet worksheet;

                try
                {
                    workbook = new XLWorkbook(stream);
                    worksheet = workbook.Worksheet(1);
                }
                catch (System.IO.FileFormatException)
                {
                    importacionDto.Estado = "Error de formato";
                    importacionDto.Errores.Add("El archivo Excel está corrupto o tiene un formato no válido. Verifique que sea un archivo .xlsx o .xls válido.");
                    return ResultadoOperacion<ImportacionExcelDTO>.Fallo("Archivo Excel inválido o corrupto", 400);
                }
                catch (ArgumentException ex) when (ex.Message.Contains("stream"))
                {
                    importacionDto.Estado = "Error de formato";
                    importacionDto.Errores.Add("No se pudo leer el archivo. Asegúrese de que sea un archivo Excel válido.");
                    return ResultadoOperacion<ImportacionExcelDTO>.Fallo("Error al leer el archivo Excel", 400);
                }

                using (workbook)
                {
                    // Validar estructura del archivo
                var headerRow = worksheet.Row(1);
                var expectedHeaders = new[] { "Enunciado", "Categoria", "Puntos", "OpcionA", "OpcionB", "OpcionC", "OpcionD", "RespuestaCorrecta", "Retroalimentacion" };

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var cellValue = headerRow.Cell(i + 1).GetString();
                    if (!string.Equals(cellValue, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        importacionDto.Errores.Add($"Encabezado incorrecto en columna {i + 1}. Se esperaba '{expectedHeaders[i]}' pero se encontró '{cellValue}'");
                    }
                }

                if (importacionDto.Errores.Any())
                {
                    importacionDto.Estado = "Error de formato";
                    return ResultadoOperacion<ImportacionExcelDTO>.Fallo("Estructura del archivo Excel incorrecta", 400);
                }

                var preguntasParaImportar = new List<BancoPregunta>();
                var filaActual = 2; // Empezar desde la segunda fila (después del encabezado)

                foreach (var row in worksheet.RowsUsed().Skip(1)) // Saltar el encabezado
                {
                    try
                    {
                        var enunciado = row.Cell(1).GetString().Trim();
                        var categoria = row.Cell(2).GetString().Trim();
                        var puntosText = row.Cell(3).GetString().Trim();
                        var opcionA = row.Cell(4).GetString().Trim();
                        var opcionB = row.Cell(5).GetString().Trim();
                        var opcionC = row.Cell(6).GetString().Trim();
                        var opcionD = row.Cell(7).GetString().Trim();
                        var respuestaCorrecta = row.Cell(8).GetString().Trim().ToUpperInvariant();
                        var retroalimentacion = row.Cell(9).GetString().Trim();

                        // Validaciones
                        var erroresFila = new List<string>();

                        if (string.IsNullOrEmpty(enunciado))
                            erroresFila.Add($"Fila {filaActual}: El enunciado es obligatorio");

                        if (string.IsNullOrEmpty(enunciado) || enunciado.Length > 1000)
                            erroresFila.Add($"Fila {filaActual}: El enunciado debe tener entre 1 y 1000 caracteres");

                        if (!string.IsNullOrEmpty(categoria) && categoria.Length > 100)
                            erroresFila.Add($"Fila {filaActual}: La categoría no puede tener más de 100 caracteres");

                        if (!int.TryParse(puntosText, out int puntos) || puntos < 1 || puntos > 100)
                            erroresFila.Add($"Fila {filaActual}: Los puntos deben ser un número entre 1 y 100");

                        if (string.IsNullOrEmpty(opcionA) || string.IsNullOrEmpty(opcionB))
                            erroresFila.Add($"Fila {filaActual}: Debe tener al menos las opciones A y B");

                        if (!new[] { "A", "B", "C", "D" }.Contains(respuestaCorrecta))
                            erroresFila.Add($"Fila {filaActual}: La respuesta correcta debe ser A, B, C o D");

                        // Verificar que la opción marcada como correcta tenga contenido
                        var opcionTextos = new Dictionary<string, string>
                        {
                            ["A"] = opcionA,
                            ["B"] = opcionB,
                            ["C"] = opcionC,
                            ["D"] = opcionD
                        };

                        if (!string.IsNullOrEmpty(respuestaCorrecta) && string.IsNullOrEmpty(opcionTextos[respuestaCorrecta]))
                            erroresFila.Add($"Fila {filaActual}: La opción {respuestaCorrecta} marcada como correcta está vacía");

                        if (!string.IsNullOrEmpty(retroalimentacion) && retroalimentacion.Length > 2000)
                            erroresFila.Add($"Fila {filaActual}: La retroalimentación no puede tener más de 2000 caracteres");

                        // Verificar duplicados
                        var existePregunta = await _context.BancoPreguntas
                            .AnyAsync(bp => bp.Texto == enunciado
                                      && bp.Categoria == categoria
                                      && bp.DocenteId == usuarioId);

                        if (existePregunta)
                            erroresFila.Add($"Fila {filaActual}: Ya existe una pregunta con el mismo enunciado en esta categoría");

                        if (erroresFila.Any())
                        {
                            importacionDto.Errores.AddRange(erroresFila);
                            importacionDto.CantidadPreguntasConErrores++;
                        }
                        else
                        {
                            // Auto-crear categoría si no existe
                            var categoriaFinal = await CrearOObtenerCategoriaAsync(categoria, usuarioId);

                            // Crear pregunta
                            var bancoPregunta = new BancoPregunta
                            {
                                Texto = enunciado,
                                Categoria = categoriaFinal,
                                DocenteId = usuarioId,
                                Puntos = puntos,
                                Activa = true,
                                Dificultad = 1, // Valor por defecto
                                TextoNormalizado = enunciado.ToLowerInvariant(),
                                FechaCreacionUtc = DateTime.UtcNow,
                                AutorId = usuarioId,
                                Retroalimentacion = string.IsNullOrWhiteSpace(retroalimentacion) ? null : retroalimentacion
                            };

                            var opciones = new List<OpcionBanco>();
                            var ordenOpcion = 1;

                            if (!string.IsNullOrEmpty(opcionA))
                            {
                                opciones.Add(new OpcionBanco
                                {
                                    Texto = opcionA,
                                    EsCorrecta = respuestaCorrecta == "A",
                                    Orden = ordenOpcion++,
                                    BancoPregunta = bancoPregunta
                                });
                            }

                            if (!string.IsNullOrEmpty(opcionB))
                            {
                                opciones.Add(new OpcionBanco
                                {
                                    Texto = opcionB,
                                    EsCorrecta = respuestaCorrecta == "B",
                                    Orden = ordenOpcion++,
                                    BancoPregunta = bancoPregunta
                                });
                            }

                            if (!string.IsNullOrEmpty(opcionC))
                            {
                                opciones.Add(new OpcionBanco
                                {
                                    Texto = opcionC,
                                    EsCorrecta = respuestaCorrecta == "C",
                                    Orden = ordenOpcion++,
                                    BancoPregunta = bancoPregunta
                                });
                            }

                            if (!string.IsNullOrEmpty(opcionD))
                            {
                                opciones.Add(new OpcionBanco
                                {
                                    Texto = opcionD,
                                    EsCorrecta = respuestaCorrecta == "D",
                                    Orden = ordenOpcion++,
                                    BancoPregunta = bancoPregunta
                                });
                            }

                            bancoPregunta.Opciones = opciones;
                            preguntasParaImportar.Add(bancoPregunta);
                        }
                    }
                    catch (Exception ex)
                    {
                        importacionDto.Errores.Add($"Fila {filaActual}: Error al procesar - {ex.Message}");
                        importacionDto.CantidadPreguntasConErrores++;
                    }

                    filaActual++;
                }

                // Guardar preguntas válidas
                if (preguntasParaImportar.Any())
                {
                    _context.BancoPreguntas.AddRange(preguntasParaImportar);
                    await _context.SaveChangesAsync();
                    importacionDto.CantidadPreguntasImportadas = preguntasParaImportar.Count;
                }

                importacionDto.Estado = importacionDto.Errores.Any() ? "Completado con errores" : "Completado exitosamente";

                var mensaje = $"Importación completada. {importacionDto.CantidadPreguntasImportadas} preguntas importadas, {importacionDto.CantidadPreguntasConErrores} con errores.";
                return ResultadoOperacion<ImportacionExcelDTO>.Exito(importacionDto, mensaje);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar preguntas desde Excel");
                importacionDto.Estado = "Error";
                importacionDto.Errores.Add($"Error general: {ex.Message}");
                return ResultadoOperacion<ImportacionExcelDTO>.Fallo("Error al procesar el archivo Excel", 500);
            }
        }

        private static BancoPreguntaDTO MapearABancoPreguntaDTO(BancoPregunta bancoPregunta)
        {
            return new BancoPreguntaDTO
            {
                Id = bancoPregunta.Id,
                Enunciado = bancoPregunta.Texto,
                Categoria = bancoPregunta.Categoria,
                DocenteId = bancoPregunta.DocenteId,
                FechaCreacion = bancoPregunta.FechaCreacion,
                FechaModificacion = bancoPregunta.FechaModificacion,
                Puntos = bancoPregunta.Puntos,
                Activa = bancoPregunta.Activa,
                Opciones = bancoPregunta.Opciones.Select(o => new OpcionBancoDTO
                {
                    Id = o.Id,
                    Texto = o.Texto,
                    EsCorrecta = o.EsCorrecta,
                    Orden = o.Orden,
                    BancoPreguntaId = o.BancoPreguntaId
                }).OrderBy(o => o.Orden).ToList()
            };
        }
    }
}