using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMSBackend.API.Services
{
    public class CursoUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CursoUsuarioService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStorageService _storageService;

        public CursoUsuarioService(ApplicationDbContext context, ILogger<CursoUsuarioService> logger, IHttpContextAccessor httpContextAccessor, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _storageService = storageService;
        }

        public async Task<ResultadoOperacion<CursoUsuarioDTO>> AsignarUsuarioACursoAsync(AsignacionUsuarioCursoDTO dto)
        {
            // Lógica va aquí
            var curso = await _context.Cursos.FindAsync(dto.CursoId);
            if (curso == null)
            {
                return ResultadoOperacion<CursoUsuarioDTO>.Fallo("Curso no encontrado", 404);
            }
            var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
            if (usuario == null)
            {
                return ResultadoOperacion<CursoUsuarioDTO>.Fallo("Usuario no encontrado", 404);
            }
            bool esRolValido = Enum.TryParse<RolEnCurso>(dto.RolEnCurso, true, out var rol);
            if (!esRolValido)
            {
                return ResultadoOperacion<CursoUsuarioDTO>.Fallo("Rol inválido. Debe ser 'Alumno' o 'Docente'", 400);
            }
            var yaAsignado = await _context.CursoUsuarios
                .AnyAsync(cu => cu.CursoId == dto.CursoId && cu.UsuarioId == dto.UsuarioId);

            if (yaAsignado)
            {
                return ResultadoOperacion<CursoUsuarioDTO>.Fallo("Ya existe una asignación para este usuario en el curso", 409);
            }
            var asignacion = new CursoUsuario
            {
                CursoId = dto.CursoId,
                UsuarioId = dto.UsuarioId,
                RolEnCurso = rol
            };
            _context.CursoUsuarios.Add(asignacion);
            await _context.SaveChangesAsync();

            var dtoRespuesta = new CursoUsuarioDTO
            {
                CursoId = curso.Nrc,
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.Nombre ?? string.Empty,
                Email = usuario.Email ?? string.Empty,
                Rut = usuario.Rut ?? string.Empty,
                RolEnCurso = rol.ToString()
            };
            return ResultadoOperacion<CursoUsuarioDTO>.Exito(dtoRespuesta, "Usuario asignado correctamente al curso");
        }
        public async Task<ResultadoOperacion<string>> EliminarAsignacionAsync(int cursoId, string usuarioId)
        {
            var asignacion = await _context.CursoUsuarios
                .FirstOrDefaultAsync(cu => cu.CursoId == cursoId && cu.UsuarioId == usuarioId);

            if (asignacion == null)
            {
                return ResultadoOperacion<string>.Fallo("Asignación no encontrada", 404);
            }

            _context.CursoUsuarios.Remove(asignacion);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<string>.Exito("Asignación eliminada correctamente");
        }
        ///
        public async Task<ResultadoOperacion<List<CursoUsuarioDTO>>> ListarUsuariosPorCursoAsync(int cursoId)
        {
            var cursoExiste = await _context.Cursos.AnyAsync(c => c.Nrc == cursoId);
            if (!cursoExiste)
            {
                return ResultadoOperacion<List<CursoUsuarioDTO>>.Fallo("Curso no encontrado", 404);
            }

            var asignaciones = await _context.CursoUsuarios
                .Where(cu => cu.CursoId == cursoId)
                .Include(cu => cu.Usuario)
                .ToListAsync();

            var resultado = asignaciones.Select(cu => new CursoUsuarioDTO
            {
                CursoId = cu.CursoId,
                UsuarioId = cu.UsuarioId,
                NombreUsuario = cu.Usuario.Nombre ?? string.Empty,
                Email = cu.Usuario.Email ?? string.Empty,
                Rut = cu.Usuario.Rut ?? string.Empty,
                RolEnCurso = cu.RolEnCurso.ToString()
            }).ToList();

            return ResultadoOperacion<List<CursoUsuarioDTO>>.Exito(resultado, "Usuarios del curso obtenidos correctamente");
        }
        public async Task<ResultadoOperacion<List<CursoDTO>>> ObtenerCursosDelUsuarioActualAsync()
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null || contexto.User == null)
            {
                return ResultadoOperacion<List<CursoDTO>>.Fallo("No se pudo obtener el contexto del usuario.", 400);
            }
            var usuarioId = contexto.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return ResultadoOperacion<List<CursoDTO>>.Fallo("No se pudo obtener el ID del usuario autenticado.", 400);
            }

            // Obtener el rol del usuario
            var rol = contexto.User.FindFirst(ClaimTypes.Role)?.Value;
            var esAdministrador = string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase);

            List<Curso> cursos;

            if (esAdministrador)
            {
                // Los administradores ven TODOS los cursos
                cursos = await _context.Cursos.ToListAsync();
            }
            else
            {
                // Docentes, Alumnos y otros roles solo ven sus cursos asignados
                cursos = await _context.CursoUsuarios
                    .Where(cu => cu.UsuarioId == usuarioId)
                    .Include(cu => cu.Curso)
                    .Select(cu => cu.Curso)
                    .ToListAsync();
            }

            var cursoDtos = new List<CursoDTO>();
            foreach (var curso in cursos)
            {
                var portadaUrl = await BuildPortadaUrlAsync(curso);
                cursoDtos.Add(new CursoDTO
                {
                    Nrc = curso.Nrc,
                    Nombre = curso.Nombre,
                    Descripcion = curso.Descripcion,
                    Activo = curso.Activo,
                    PortadaUrl = portadaUrl,
                    PortadaActualizada = curso.PortadaActualizada
                });
            }

            return ResultadoOperacion<List<CursoDTO>>.Exito(cursoDtos, "Cursos asignados obtenidos correctamente");
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
