using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMSBackend.API.Services
{
    public class ForoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ForoService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // =========================================
        // CrearForoAsync (usa ForoCreacionDTO)
        // =========================================
        public async Task<ResultadoOperacion<ForoDTO>> CrearForoAsync(ForoCreacionDTO dto)
        {
            // Validaciones básicas (además de DataAnnotations)
            if (string.IsNullOrWhiteSpace(dto.Titulo) || dto.Titulo.Trim().Length > 120)
                return ResultadoOperacion<ForoDTO>.Fallo("Título vacío o supera 120 caracteres.", 400);
            if (!string.IsNullOrWhiteSpace(dto.Descripcion) && dto.Descripcion.Trim().Length > 2000)
                return ResultadoOperacion<ForoDTO>.Fallo("Descripción supera 2000 caracteres.", 400);

            // Módulo
            var modulo = await _context.Modulos.FirstOrDefaultAsync(m => m.ModuloId == dto.ModuloId);
            if (modulo == null)
                return ResultadoOperacion<ForoDTO>.Fallo("Módulo no encontrado.", 404);

            // Contexto/usuario
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<ForoDTO>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<ForoDTO>.Fallo("Usuario no encontrado", 400);

            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado", 401);

            // Autorización: Docente del curso o Admin global
            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rol == "Administrador");
            if (!esDocente && !esAdmin)
                return ResultadoOperacion<ForoDTO>.Fallo("No autorizado para crear foros en este curso.", 403);

            // Crear entidad (Estado y flags vienen por defaults del mapeo)
            var foro = new Foro
            {
                ModuloId = dto.ModuloId,
                Modulo = modulo,
                Titulo = dto.Titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                CreadorId = userId,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Foros.Add(foro);
            await _context.SaveChangesAsync();

            var dato = new ForoDTO
            {
                ForoId = foro.ForoId,
                ModuloId = foro.ModuloId,
                Titulo = foro.Titulo,
                Descripcion = foro.Descripcion,
                Estado = foro.Estado.ToString(),
                AllowStudentThreads = foro.AllowStudentThreads,
                RequireInitialPostToView = foro.RequireInitialPostToView,
                CreadorId = foro.CreadorId,
                FechaCreacion = foro.FechaCreacion
            };

            return ResultadoOperacion<ForoDTO>.Exito(dato);
        }

        // =========================================
        // EditarForoAsync (usa ForoEdicionDTO)
        // =========================================
        public async Task<ResultadoOperacion<ForoDTO>> EditarForoAsync(int foroId, ForoEdicionDTO dto)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<ForoDTO>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<ForoDTO>.Fallo("Usuario no encontrado", 400);

            var foro = await _context.Foros.Include(f => f.Creador)
                                           .FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null) return ResultadoOperacion<ForoDTO>.Fallo("Foro no encontrado.", 404);

            var modulo = await _context.Modulos.FirstOrDefaultAsync(m => m.ModuloId == foro.ModuloId);
            if (modulo == null) return ResultadoOperacion<ForoDTO>.Fallo("No se pudo resolver el módulo del foro.", 409);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado", 401);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rol == "Administrador");
            if (!esDocente && !esAdmin) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado.", 403);

            if (dto.Titulo != null)
            {
                var t = dto.Titulo.Trim();
                if (string.IsNullOrWhiteSpace(t) || t.Length > 120)
                    return ResultadoOperacion<ForoDTO>.Fallo("Título vacío o supera 120 caracteres.", 400);
                foro.Titulo = t;
            }

            if (dto.Descripcion != null)
            {
                var d = dto.Descripcion.Trim();
                if (d.Length > 2000)
                    return ResultadoOperacion<ForoDTO>.Fallo("Descripción supera 2000 caracteres.", 400);
                foro.Descripcion = string.IsNullOrWhiteSpace(d) ? null : d;
            }

            await _context.SaveChangesAsync();

            var dato = new ForoDTO
            {
                ForoId = foro.ForoId,
                ModuloId = foro.ModuloId,
                Titulo = foro.Titulo,
                Descripcion = foro.Descripcion,
                Estado = foro.Estado.ToString(),
                AllowStudentThreads = foro.AllowStudentThreads,
                RequireInitialPostToView = foro.RequireInitialPostToView,
                CreadorId = foro.CreadorId,
                CreadorNombre = foro.Creador?.Nombre,
                FechaCreacion = foro.FechaCreacion
            };

            return ResultadoOperacion<ForoDTO>.Exito(dato);
        }

        // =========================================
        // CambiarEstadoForoAsync (usa ForoCambioEstadoDTO)
        // =========================================
        public async Task<ResultadoOperacion<ForoDTO>> CambiarEstadoForoAsync(int foroId, ForoCambioEstadoDTO dto)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<ForoDTO>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<ForoDTO>.Fallo("Usuario no encontrado", 400);

            var foro = await _context.Foros.Include(f => f.Creador)
                                           .FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null) return ResultadoOperacion<ForoDTO>.Fallo("Foro no encontrado.", 404);

            var modulo = await _context.Modulos.FirstOrDefaultAsync(m => m.ModuloId == foro.ModuloId);
            if (modulo == null) return ResultadoOperacion<ForoDTO>.Fallo("No se pudo resolver el módulo del foro.", 409);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado", 401);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rol == "Administrador");
            if (!esDocente && !esAdmin) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado.", 403);

            // Parse y validación de transición
            if (!Enum.TryParse<Estado>(dto.NuevoEstado, true, out var nuevo))
                return ResultadoOperacion<ForoDTO>.Fallo("Estado inválido. Usa: Activo, Cerrado o Archivado.", 400);

            var actual = foro.Estado;
            var transicionValida =
                (actual == Estado.Activo && (nuevo == Estado.Cerrado || nuevo == Estado.Archivado)) ||
                ((actual == Estado.Cerrado || actual == Estado.Archivado) && nuevo == Estado.Activo);
            if (!transicionValida)
                return ResultadoOperacion<ForoDTO>.Fallo("Transición de estado no permitida.", 400);

            foro.Estado = nuevo;
            await _context.SaveChangesAsync();

            var dato = new ForoDTO
            {
                ForoId = foro.ForoId,
                ModuloId = foro.ModuloId,
                Titulo = foro.Titulo,
                Descripcion = foro.Descripcion,
                Estado = foro.Estado.ToString(),
                AllowStudentThreads = foro.AllowStudentThreads,
                RequireInitialPostToView = foro.RequireInitialPostToView,
                CreadorId = foro.CreadorId,
                CreadorNombre = foro.Creador?.Nombre,
                FechaCreacion = foro.FechaCreacion
            };

            return ResultadoOperacion<ForoDTO>.Exito(dato);
        }

        // =========================================
        // ActualizarPoliticasForoAsync (usa ForoPoliciesDTO)
        // =========================================
        public async Task<ResultadoOperacion<ForoDTO>> ActualizarPoliticasForoAsync(int foroId, ForoPoliciesDTO dto)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<ForoDTO>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<ForoDTO>.Fallo("Usuario no encontrado", 400);

            var foro = await _context.Foros.Include(f => f.Creador)
                                           .FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null) return ResultadoOperacion<ForoDTO>.Fallo("Foro no encontrado.", 404);

            var modulo = await _context.Modulos.FirstOrDefaultAsync(m => m.ModuloId == foro.ModuloId);
            if (modulo == null) return ResultadoOperacion<ForoDTO>.Fallo("No se pudo resolver el módulo del foro.", 409);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado", 401);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rol == "Administrador");
            if (!esDocente && !esAdmin) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado.", 403);

            if (dto.AllowStudentThreads.HasValue)
                foro.AllowStudentThreads = dto.AllowStudentThreads.Value;
            if (dto.RequireInitialPostToView.HasValue)
                foro.RequireInitialPostToView = dto.RequireInitialPostToView.Value;

            await _context.SaveChangesAsync();

            var dato = new ForoDTO
            {
                ForoId = foro.ForoId,
                ModuloId = foro.ModuloId,
                Titulo = foro.Titulo,
                Descripcion = foro.Descripcion,
                Estado = foro.Estado.ToString(),
                AllowStudentThreads = foro.AllowStudentThreads,
                RequireInitialPostToView = foro.RequireInitialPostToView,
                CreadorId = foro.CreadorId,
                CreadorNombre = foro.Creador?.Nombre,
                FechaCreacion = foro.FechaCreacion
            };

            return ResultadoOperacion<ForoDTO>.Exito(dato);
        }

        // =========================================
        // ObtenerForoAsync (por Id)
        // =========================================
        public async Task<ResultadoOperacion<ForoDTO>> ObtenerForoAsync(int foroId)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<ForoDTO>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<ForoDTO>.Fallo("Usuario no encontrado", 400);

            var foro = await _context.Foros.AsNoTracking()
                                           .Include(f => f.Creador)
                                           .FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null) return ResultadoOperacion<ForoDTO>.Fallo("Foro no encontrado.", 404);

            var modulo = await _context.Modulos.AsNoTracking().FirstOrDefaultAsync(m => m.ModuloId == foro.ModuloId);
            if (modulo == null) return ResultadoOperacion<ForoDTO>.Fallo("No se pudo resolver el módulo del foro.", 409);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado", 401);

            var esMiembro = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId);
            var esAdmin = (rol == "Administrador");
            if (!esMiembro && !esAdmin) return ResultadoOperacion<ForoDTO>.Fallo("No autorizado.", 403);

            var dato = new ForoDTO
            {
                ForoId = foro.ForoId,
                ModuloId = foro.ModuloId,
                Titulo = foro.Titulo,
                Descripcion = foro.Descripcion,
                Estado = foro.Estado.ToString(),
                AllowStudentThreads = foro.AllowStudentThreads,
                RequireInitialPostToView = foro.RequireInitialPostToView,
                CreadorId = foro.CreadorId,
                CreadorNombre = foro.Creador?.Nombre,
                FechaCreacion = foro.FechaCreacion
            };

            return ResultadoOperacion<ForoDTO>.Exito(dato);
        }

        // =====================================================================
        // ListarForosPorModuloAsync (usa ForoListadoDTO con filtros y paginación)
        // =====================================================================
        public async Task<ResultadoOperacion<Page<ForoListItemDTO>>> ListarForosPorModuloAsync(ForoListadoDTO dto)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("Usuario no encontrado", 400);

            if (dto.Pagina < 1) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("La página debe ser >= 1.", 400);
            if (dto.CantidadPorPagina < 1 || dto.CantidadPorPagina > 50)
                return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("La cantidad por página debe estar entre 1 y 50.", 400);

            var modulo = await _context.Modulos.AsNoTracking().FirstOrDefaultAsync(m => m.ModuloId == dto.ModuloId);
            if (modulo == null) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("Módulo no encontrado.", 404);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("No autorizado", 401);

            var esMiembro = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId);
            var esAdmin = (rol == "Administrador");
            if (!esMiembro && !esAdmin) return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("No autorizado.", 403);

            var query = _context.Foros.AsNoTracking().Where(f => f.ModuloId == dto.ModuloId);

            // Estado (string -> enum)
            if (!string.IsNullOrWhiteSpace(dto.Estado))
            {
                if (!Enum.TryParse<Estado>(dto.Estado, true, out var estadoEnum))
                    return ResultadoOperacion<Page<ForoListItemDTO>>.Fallo("Estado inválido.", 400);
                query = query.Where(f => f.Estado == estadoEnum);
            }
            else if (!dto.IncluirArchivados)
            {
                query = query.Where(f => f.Estado != Estado.Archivado);
            }

            // Búsqueda por título
            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var ql = dto.Q.Trim().ToLower();
                query = query.Where(f => f.Titulo.ToLower().Contains(ql));
            }

            // Orden sugerido: más nuevos primero
            query = query.OrderByDescending(f => f.FechaCreacion);

            var total = await query.CountAsync();
            var registros = await query
                .Skip((dto.Pagina - 1) * dto.CantidadPorPagina)
                .Take(dto.CantidadPorPagina)
                .ToListAsync();

            var items = registros.Select(f => new ForoListItemDTO
            {
                ForoId = f.ForoId,
                ModuloId = f.ModuloId,
                Titulo = f.Titulo,
                Estado = f.Estado.ToString(),
                FechaCreacion = f.FechaCreacion
            }).ToList();

            var page = new Page<ForoListItemDTO>
            {
                PageNumber = dto.Pagina,
                PageSize = dto.CantidadPorPagina,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)dto.CantidadPorPagina),
                Items = items
            };

            return ResultadoOperacion<Page<ForoListItemDTO>>.Exito(page);
        }

        // =========================================
        // EliminarForoAsync
        // =========================================
        public async Task<ResultadoOperacion<string>> EliminarForoAsync(int foroId)
        {
            var contexto = _httpContextAccessor.HttpContext;
            if (contexto == null) return ResultadoOperacion<string>.Fallo("No hay contexto", 400);
            var usuario = contexto.User;
            if (usuario == null) return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 400);

            var foro = await _context.Foros.FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null) return ResultadoOperacion<string>.Fallo("Foro no encontrado.", 404);

            var modulo = await _context.Modulos.FirstOrDefaultAsync(m => m.ModuloId == foro.ModuloId);
            if (modulo == null) return ResultadoOperacion<string>.Fallo("No se pudo resolver el módulo del foro.", 409);

            var userId = usuario.FindFirst("sub")?.Value ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<string>.Fallo("No autorizado", 401);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == modulo.CursoId && cu.UsuarioId == userId && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rol == "Administrador");
            if (!esDocente && !esAdmin) return ResultadoOperacion<string>.Fallo("No autorizado.", 403);

            _context.Foros.Remove(foro);
            await _context.SaveChangesAsync(); // cascada borra Hilos y Posts

            return ResultadoOperacion<string>.Exito("Foro eliminado correctamente.");
        }
    }
}
