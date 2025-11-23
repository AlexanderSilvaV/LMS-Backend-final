using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace LMSBackend.API.Services
{
    public class CursoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CursoService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStorageService _storageService;
    private static readonly string[] TiposPortadaPermitidos = { "image/jpeg", "image/png", "image/webp" };
        private const int MaxPortadaBytes = 5 * 1024 * 1024;

        public CursoService(ApplicationDbContext context, ILogger<CursoService> logger, IHttpContextAccessor httpContextAccessor, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _storageService = storageService;
        }
        public async Task<ResultadoOperacion<CursoDTO>> CrearCursoAsync(CursoCreacionDTO dto)
        {
            var existeNrc = await _context.Cursos.AnyAsync(c => c.Nrc == dto.Nrc);
            if (existeNrc)
            {
                return ResultadoOperacion<CursoDTO>.Fallo(mensaje: "Nrc ya registrado", 409);
            }
            var nombre = string.IsNullOrWhiteSpace(dto.Nombre);
            if (nombre)
            {
                return ResultadoOperacion<CursoDTO>.Fallo(mensaje: "Nombre no puede estar vacio", 400);
            }
            var descripcion = string.IsNullOrWhiteSpace(dto.Descripcion);
            if (descripcion)
            {
                return ResultadoOperacion<CursoDTO>.Fallo(mensaje: "Descripcion no puede estar vacia", 400);
            }
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo(mensaje: "No hay contexto", 400);
            }
            var usuario = contexto.User;
            if (usuario == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo(mensaje: "Usuario no encontrado", 400);
            }
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (rol != "Administrador")
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 403);
            }
            var adminId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(adminId))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 401);
            }

            var curso = new Curso
            {
                Nrc = dto.Nrc,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo,
                AdministradorId = adminId
            };

            var nombres = new List<string> { "Información General", "Directrices y Reglamentos", "Integridad Académica" };
            for (int i = 0; i < nombres.Count; i++)
            {
                var modulo = new Modulo
                {
                    Nombre = nombres[i],
                    Indice = i + 1,
                    EsPredeterminado = true,
                };

                curso.Modulos.Add(modulo);
            }
            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            var cursoDto = MapCursoToDto(curso);

            return ResultadoOperacion<CursoDTO>.Exito(cursoDto);
        }
        public async Task<ResultadoOperacion<CursoBusquedaResultadoDTO>> BuscarCursosAsync(CursoBusquedaDTO dto)
        {
            if (dto.Pagina < 1)
            {
                return ResultadoOperacion<CursoBusquedaResultadoDTO>.Fallo("La página debe ser mayor o igual a 1", 400);
            }
            if (dto.CantidadPorPagina < 1 || dto.CantidadPorPagina > 50)
            {
                return ResultadoOperacion<CursoBusquedaResultadoDTO>.Fallo("La cantidad por página debe estar entre 1 y 50", 400);
            }
            var query = _context.Cursos.AsQueryable();

            if (dto.Nrc.HasValue)
            {
                query = query.Where(c => c.Nrc == dto.Nrc.Value);
            }
            if (dto.Activo.HasValue)
            {
                query = query.Where(c => c.Activo == dto.Activo.Value);
            }
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                query = query.Where(c => c.Nombre.ToLower().Contains(dto.Nombre.ToLower()));
            }
            var totalResultados = await query.CountAsync();

            var cursos = await query
            .Skip((dto.Pagina - 1) * dto.CantidadPorPagina)
            .Take(dto.CantidadPorPagina)
            .ToListAsync();

            var cursosDTO = new List<CursoDTO>();
            foreach (var curso in cursos)
            {
                var portadaUrl = await BuildPortadaUrlAsync(curso);
                cursosDTO.Add(new CursoDTO
                {
                    Nrc = curso.Nrc,
                    Nombre = curso.Nombre,
                    Descripcion = curso.Descripcion,
                    Activo = curso.Activo,
                    PortadaUrl = portadaUrl,
                    PortadaActualizada = curso.PortadaActualizada
                });
            }

            var resultado = new CursoBusquedaResultadoDTO
            {
                Cursos = cursosDTO,
                Paginacion = new PaginacionDTO
                {
                    PaginaActual = dto.Pagina,
                    CantidadPorPagina = dto.CantidadPorPagina,
                    TotalResultados = totalResultados,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)dto.CantidadPorPagina)
                }
            };

            return ResultadoOperacion<CursoBusquedaResultadoDTO>.Exito(resultado);
        }
        public async Task<ResultadoOperacion<string>> EliminarCursoAsync(int nrc)
        {
            var curso = await _context.Cursos.FindAsync(nrc);
            if (curso == null)
            {
                return ResultadoOperacion<string>.Fallo("Curso no encontrado", 404);
            }

            if (!string.IsNullOrEmpty(curso.PortadaFileId))
            {
                try
                {
                    await _storageService.DeleteFileAsync(curso.PortadaFileId, "course-covers");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo eliminar la portada del curso {Nrc} desde el almacenamiento", nrc);
                }
            }

            _context.Cursos.Remove(curso);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<string>.Exito("Curso eliminado correctamente");
        }
        public async Task<ResultadoOperacion<CursoDTO>> EditarCursoAsync(int nrc, CursoEdicionDTO dto)
        {
            var curso = await _context.Cursos.FindAsync(nrc);

            // Si no se encuentra, retornar error 404
            if (curso == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("Curso no encontrado", 404);
            }

            // Validar los datos de entrada
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El nombre no puede estar vacío", 400);
            }

            if (string.IsNullOrWhiteSpace(dto.Descripcion))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("La descripción no puede estar vacía", 400);
            }

            // Actualizar los campos del curso
            curso.Nombre = dto.Nombre.Trim();
            curso.Descripcion = dto.Descripcion.Trim();
            curso.Activo = dto.Activo;

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            // Devolver el curso actualizado como DTO
            var cursoActualizado = MapCursoToDto(curso);

            return ResultadoOperacion<CursoDTO>.Exito(cursoActualizado);
        }

        public async Task<ResultadoOperacion<CursoDTO>> ActualizarPortadaAsync(int nrc, IFormFile archivo)
        {
            var (userId, rol) = ObtenerUsuarioActual();

            if (string.IsNullOrEmpty(userId))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 401);
            }

            if (archivo == null || archivo.Length == 0)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El archivo está vacío", 400);
            }

            if (!TiposPortadaPermitidos.Contains(archivo.ContentType))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("Formato de imagen no permitido", 415);
            }

            if (archivo.Length > MaxPortadaBytes)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El archivo excede el límite de 5MB", 400);
            }

            var curso = await _context.Cursos
                .Include(c => c.CursoUsuarios)
                .FirstOrDefaultAsync(c => c.Nrc == nrc);

            if (curso == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("Curso no encontrado", 404);
            }

            var esAdmin = string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase);
            var esDocenteRol = string.Equals(rol, "Docente", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(rol, "Profesor", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(rol, "Teacher", StringComparison.OrdinalIgnoreCase);
            var esDocenteAsignado = esAdmin || esDocenteRol || await _context.CursoUsuarios
                .AnyAsync(cu => cu.CursoId == nrc && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);

            if (!esDocenteAsignado)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 403);
            }

            var processedStream = new MemoryStream();

            try
            {
                using var image = Image.Load(archivo.OpenReadStream());

                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1280, 720)
                };

                image.Mutate(x => x.Resize(resizeOptions));

                processedStream.SetLength(0);
                image.Save(processedStream, new PngEncoder());
                processedStream.Position = 0;

                var sanitizedFileName = Path.GetFileNameWithoutExtension(archivo.FileName);
                if (string.IsNullOrWhiteSpace(sanitizedFileName))
                {
                    sanitizedFileName = $"portada-{nrc}";
                }

                var processedFile = new FormFile(processedStream, 0, processedStream.Length, "archivo", $"{sanitizedFileName}.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                };

                // Usar "system" como userId para que las portadas se almacenen centralizadamente
                // y coincidan con la ruta usada al generar URLs presignadas en CursoUsuarioService
                var (fileId, fileUrl) = await _storageService.SaveFileAsync("system", processedFile, "course-covers");
                var previousFileId = curso.PortadaFileId;

                curso.PortadaFileId = fileId;
                curso.PortadaUrl = fileUrl;
                curso.PortadaActualizada = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(previousFileId) && !string.Equals(previousFileId, fileId, StringComparison.Ordinal))
                {
                    try
                    {
                        await _storageService.DeleteFileAsync(previousFileId, "course-covers");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo eliminar la portada anterior del curso {Nrc}", nrc);
                    }
                }

                var dto = MapCursoToDto(curso);
                return ResultadoOperacion<CursoDTO>.Exito(dto, "Portada actualizada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la portada del curso {Nrc}", nrc);
                return ResultadoOperacion<CursoDTO>.Fallo("Error al procesar la imagen", 500);
            }
            finally
            {
                processedStream.Dispose();
            }
        }

        public async Task<ResultadoOperacion<string>> EliminarPortadaAsync(int nrc)
        {
            var (userId, rol) = ObtenerUsuarioActual();

            if (string.IsNullOrEmpty(userId))
            {
                return ResultadoOperacion<string>.Fallo("No autorizado", 401);
            }

            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.Nrc == nrc);

            if (curso == null)
            {
                return ResultadoOperacion<string>.Fallo("Curso no encontrado", 404);
            }

            var esAdmin = string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase);
            var esDocenteRol = string.Equals(rol, "Docente", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(rol, "Profesor", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(rol, "Teacher", StringComparison.OrdinalIgnoreCase);
            var esDocenteAsignado = esAdmin || esDocenteRol || await _context.CursoUsuarios
                .AnyAsync(cu => cu.CursoId == nrc && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);

            if (!esDocenteAsignado)
            {
                return ResultadoOperacion<string>.Fallo("No autorizado", 403);
            }

            if (string.IsNullOrEmpty(curso.PortadaFileId))
            {
                return ResultadoOperacion<string>.Fallo("El curso no tiene portada", 404);
            }

            var portadaFileId = curso.PortadaFileId;

            curso.PortadaFileId = null;
            curso.PortadaUrl = null;
            curso.PortadaActualizada = null;

            await _context.SaveChangesAsync();

            try
            {
                await _storageService.DeleteFileAsync(portadaFileId, "course-covers");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar la portada del curso {Nrc} desde el almacenamiento", nrc);
            }

            return ResultadoOperacion<string>.Exito("Portada eliminada correctamente");
        }

        public async Task<ResultadoOperacion<CursoPortadaDTO>> ObtenerPortadaAsync(int nrc)
        {
            var curso = await _context.Cursos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Nrc == nrc);

            if (curso == null)
            {
                return ResultadoOperacion<CursoPortadaDTO>.Fallo("Curso no encontrado", 404);
            }

            if (string.IsNullOrEmpty(curso.PortadaFileId))
            {
                return ResultadoOperacion<CursoPortadaDTO>.Fallo("El curso no tiene portada", 404);
            }

            // Generar una nueva URL prefirmada de S3 (válida por 15 minutos)
            string portadaUrl;
            try
            {
                // Si el storage service es S3StorageService, generar URL prefirmada
                if (_storageService is S3StorageService s3Service)
                {
                    var downloadResponse = await s3Service.GeneratePresignedDownloadUrlAsync(
                        "system",
                        new DTOs.GenerateDownloadUrlRequestDTO
                        {
                            FileId = curso.PortadaFileId,
                            Subfolder = "course-covers"
                        }
                    );
                    portadaUrl = downloadResponse.PresignedUrl;
                }
                else
                {
                    // Para LocalStorageService, usar la URL almacenada
                    portadaUrl = curso.PortadaUrl ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL de portada para curso {Nrc}", nrc);
                return ResultadoOperacion<CursoPortadaDTO>.Fallo("Error al obtener la portada", 500);
            }

            if (string.IsNullOrEmpty(portadaUrl))
            {
                return ResultadoOperacion<CursoPortadaDTO>.Fallo("La portada no está disponible", 404);
            }

            var dto = new CursoPortadaDTO
            {
                Url = portadaUrl,
                ActualizadaEn = curso.PortadaActualizada ?? DateTime.UtcNow
            };

            return ResultadoOperacion<CursoPortadaDTO>.Exito(dto);
        }

        public async Task<ResultadoOperacion<CursoDTO>> DuplicarCursoAsync(CursoDuplicacionDTO dto)
        {
            // Validar autorización
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No hay contexto", 400);
            }

            var usuario = contexto.User;
            if (usuario == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("Usuario no encontrado", 400);
            }

            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (rol != "Administrador")
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 403);
            }

            var adminId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("No autorizado", 401);
            }

            // Validar que el NRC original y nuevo sean diferentes
            if (dto.NrcOriginal == dto.NuevoNrc)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El NRC original y el nuevo NRC deben ser diferentes", 400);
            }

            // Validar que el nuevo NRC no exista
            var existeNuevoNrc = await _context.Cursos.AnyAsync(c => c.Nrc == dto.NuevoNrc);
            if (existeNuevoNrc)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El nuevo NRC ya está registrado", 409);
            }

            // Validar datos de entrada
            if (string.IsNullOrWhiteSpace(dto.NuevoNombre))
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El nuevo nombre no puede estar vacío", 400);
            }

            if (dto.NuevoNombre.Trim().Length < 3)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("El nombre debe tener al menos 3 caracteres", 400);
            }

            // Obtener el curso original con sus módulos y materiales
            var cursoOriginal = await _context.Cursos
                .Include(c => c.Modulos)
                    .ThenInclude(m => m.Materiales)
                .FirstOrDefaultAsync(c => c.Nrc == dto.NrcOriginal);

            if (cursoOriginal == null)
            {
                return ResultadoOperacion<CursoDTO>.Fallo("Curso original no encontrado", 404);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Crear el nuevo curso (sin estudiantes enrollados)
                var nuevoCurso = new Curso
                {
                    Nrc = dto.NuevoNrc,
                    Nombre = dto.NuevoNombre.Trim(),
                    Descripcion = !string.IsNullOrWhiteSpace(dto.NuevaDescripcion) 
                        ? dto.NuevaDescripcion.Trim() 
                        : cursoOriginal.Descripcion,
                    Activo = dto.Activo,
                    AdministradorId = adminId,
                    PortadaFileId = cursoOriginal.PortadaFileId,
                    PortadaUrl = cursoOriginal.PortadaUrl,
                    PortadaActualizada = cursoOriginal.PortadaUrl != null ? DateTime.UtcNow : null
                };

                _context.Cursos.Add(nuevoCurso);
                await _context.SaveChangesAsync();

                // Duplicar módulos y sus materiales
                var modulosOrdenados = cursoOriginal.Modulos.OrderBy(m => m.Indice).ToList();
                
                foreach (var moduloOriginal in modulosOrdenados)
                {
                    var nuevoModulo = new Modulo
                    {
                        Nombre = moduloOriginal.Nombre,
                        Indice = moduloOriginal.Indice,
                        EsPredeterminado = moduloOriginal.EsPredeterminado,
                        CursoId = nuevoCurso.Nrc
                    };

                    _context.Modulos.Add(nuevoModulo);
                    await _context.SaveChangesAsync();

                    // Duplicar materiales del módulo
                    foreach (var materialOriginal in moduloOriginal.Materiales)
                    {
                        var nuevoMaterial = new Material
                        {
                            Nombre = materialOriginal.Nombre,
                            Tipo = materialOriginal.Tipo,
                            Ruta = await DuplicarRutaMaterialAsync(materialOriginal),
                            ModuloId = nuevoModulo.ModuloId,
                            UsuarioId = adminId
                        };

                        _context.Materiales.Add(nuevoMaterial);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                var cursoDTO = MapCursoToDto(nuevoCurso);

                return ResultadoOperacion<CursoDTO>.Exito(cursoDTO, "Curso duplicado exitosamente");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al duplicar curso {NrcOriginal} a {NuevoNrc}", dto.NrcOriginal, dto.NuevoNrc);
                return ResultadoOperacion<CursoDTO>.Fallo("Error interno del servidor al duplicar curso", 500);
            }
        }

        private async Task<string> DuplicarRutaMaterialAsync(Material materialOriginal)
        {
            // Para archivos, necesitamos duplicar el archivo físico
            if (materialOriginal.Tipo == TipoMaterial.Archivo && !string.IsNullOrEmpty(materialOriginal.Ruta))
            {
                try
                {
                    // Extraer el nombre del archivo de la ruta original
                    var nombreArchivo = Path.GetFileName(materialOriginal.Ruta);
                    
                    // Usar el servicio de storage para copiar el archivo
                    var (nuevoFileId, nuevaFileUrl) = await _storageService.CopyFileAsync(nombreArchivo, "materials");
                    
                    return nuevaFileUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo duplicar archivo {Ruta}, usando ruta original", materialOriginal.Ruta);
                    return materialOriginal.Ruta;
                }
            }

            // Para enlaces y videos, mantener la misma ruta
            return materialOriginal.Ruta;
        }

        private CursoDTO MapCursoToDto(Curso curso)
        {
            return new CursoDTO
            {
                Nrc = curso.Nrc,
                Nombre = curso.Nombre,
                Descripcion = curso.Descripcion,
                Activo = curso.Activo,
                PortadaUrl = ComposePortadaUrl(curso),
                PortadaActualizada = curso.PortadaActualizada
            };
        }

        private string? ComposePortadaUrl(Curso curso)
        {
            if (string.IsNullOrEmpty(curso.PortadaFileId) || string.IsNullOrEmpty(curso.PortadaUrl))
            {
                return null;
            }

            if (curso.PortadaActualizada.HasValue)
            {
                return $"{curso.PortadaUrl}?v={curso.PortadaActualizada.Value.Ticks}";
            }

            return curso.PortadaUrl;
        }

        private (string? userId, string? rol) ObtenerUsuarioActual()
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto?.User == null)
            {
                return (null, null);
            }

            var usuario = contexto.User;
            var userId = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? usuario.FindFirst("sub")?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;

            return (userId, rol);
        }

        private async Task<string?> BuildPortadaUrlAsync(Curso curso)
        {
            // Si no hay FileId de portada, retornar null
            if (string.IsNullOrEmpty(curso.PortadaFileId))
            {
                return null;
            }

            try
            {
                // Si el storage service es S3StorageService, generar URL prefirmada fresca
                if (_storageService is S3StorageService s3Service)
                {
                    var downloadResponse = await s3Service.GeneratePresignedDownloadUrlAsync(
                        "system",
                        new GenerateDownloadUrlRequestDTO
                        {
                            FileId = curso.PortadaFileId,
                            Subfolder = "course-covers"
                        }
                    );
                    return downloadResponse.PresignedUrl;
                }
                else
                {
                    // Para LocalStorageService, usar la URL almacenada
                    if (string.IsNullOrEmpty(curso.PortadaUrl))
                    {
                        return null;
                    }

                    return curso.PortadaActualizada.HasValue
                        ? $"{curso.PortadaUrl}?v={curso.PortadaActualizada.Value.Ticks}"
                        : curso.PortadaUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL de portada para curso {Nrc}", curso.Nrc);
                return null;
            }
        }

    }
}
