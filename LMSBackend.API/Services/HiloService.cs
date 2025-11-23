using LMSBackend.API.Data;
using LMSBackend.API.Helpers;
using LMSBackend.API.DTOs;
using LMSBackend.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMSBackend.API.Services
{
    public class HiloService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;
        private readonly EmailService _emailService;
        private readonly ILogger<HiloService> _logger;

        public HiloService(ApplicationDbContext context, IHttpContextAccessor http, EmailService emailService, ILogger<HiloService> logger)
        {
            _context = context;
            _http = http;
            _emailService = emailService;
            _logger = logger;
        }

        // ==========================================================
        // CrearHilo(foroId, titulo, autorId)
        // Reglas:
        //  - 404 si el foro no existe
        //  - Foro.Estado debe ser Activo (409 si no)
        //  - Si autor es Alumno y AllowStudentThreads == false -> 403
        //  - Crear con: Pinned=false, Cerrado=false, LastActivityAt=UtcNow
        //  - Errores: 404, 403, 409
        // ==========================================================
        public async Task<ResultadoOperacion<HiloDTO>> CrearHiloAsync(int foroId, string titulo, string autorId)
        {
            // 1) Validaciones de entrada mínimas
            if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 120)
                return ResultadoOperacion<HiloDTO>.Fallo("Título inválido (vacío o >120).", 400);

            // 2) Cargar foro
            var foro = await _context.Foros
                .Include(f => f.Modulo)
                .FirstOrDefaultAsync(f => f.ForoId == foroId);

            if (foro == null)
                return ResultadoOperacion<HiloDTO>.Fallo("Foro no encontrado.", 404);

            // 3) Foro debe estar Activo
            if (foro.Estado != Estado.Activo) // Activo/Cerrado/Archivado
                return ResultadoOperacion<HiloDTO>.Fallo("El foro no está activo.", 409); // regla pedida

            // 4) Si autor es alumno y AllowStudentThreads = false => 403
            //    Determinamos el rol del autor en el curso del foro.
            var esAlumno = await _context.CursoUsuarios
                .AsNoTracking()
                .AnyAsync(cu => cu.CursoId == foro.Modulo.CursoId
                                && cu.UsuarioId == autorId
                                && cu.RolEnCurso == RolEnCurso.Alumno);

            if (esAlumno && !foro.AllowStudentThreads) // flag por default true en mapeo
                return ResultadoOperacion<HiloDTO>.Fallo("Los alumnos no pueden crear hilos en este foro.", 403);

            // (Opcional) si quieres negar creación a quienes no pertenecen al curso:
            var perteneceAlCurso = await _context.CursoUsuarios
                .AsNoTracking()
                .AnyAsync(cu => cu.CursoId == foro.Modulo.CursoId && cu.UsuarioId == autorId);
            if (!perteneceAlCurso)
                return ResultadoOperacion<HiloDTO>.Fallo("No perteneces al curso del foro.", 403);

            // 5) Crear hilo (Pinned/Cerrado defaults ya están mapeados, pero seteamos explícito para claridad)
            var ahora = DateTime.UtcNow;
            var hilo = new Hilo
            {
                ForoId = foroId,
                Foro = foro,
                Titulo = titulo.Trim(),
                AutorId = autorId,
                Pinned = false,
                PinnedOrder = null,
                Cerrado = false,
                LastActivityAt = ahora,
                FechaCreacion = ahora
            };

            _context.Hilos.Add(hilo);
            await _context.SaveChangesAsync();

            // Enviar notificaciones por email a los participantes del curso
            try
            {
                var autor = await _context.Usuarios.FindAsync(autorId);
                var curso = await _context.Cursos.FindAsync(foro.Modulo.CursoId);

                // Obtener todos los estudiantes del curso excepto el autor
                var estudiantesDelCurso = await _context.CursoUsuarios
                    .Include(cu => cu.Usuario)
                    .Where(cu => cu.CursoId == foro.Modulo.CursoId
                              && cu.RolEnCurso == RolEnCurso.Alumno
                              && cu.UsuarioId != autorId)
                    .Select(cu => cu.Usuario)
                    .ToListAsync();

                var enlaceHilo = $"http://135.148.148.88:3000/foros/{foroId}/hilos/{hilo.HiloId}";

                foreach (var estudiante in estudiantesDelCurso)
                {
                    if (!string.IsNullOrEmpty(estudiante.Email))
                    {
                        await _emailService.EnviarNotificacionPublicacionHiloAsync(
                            destinatario: estudiante.Email,
                            nombreUsuario: estudiante.Nombre,
                            nombreCurso: curso?.Nombre ?? "Curso",
                            tituloForo: foro.Titulo,
                            tituloHilo: hilo.Titulo,
                            autorHilo: autor?.Nombre ?? "Usuario",
                            enlaceHilo: enlaceHilo
                        );
                    }
                }

                _logger.LogInformation($"Notificaciones enviadas para nuevo hilo: {hilo.Titulo} ({estudiantesDelCurso.Count} destinatarios)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al enviar notificaciones para hilo {hilo.HiloId}: {ex.Message}");
                // No bloqueamos la creación del hilo si falla el envío de emails
            }

            var dto = new HiloDTO
            {
                HiloId = hilo.HiloId,
                ForoId = hilo.ForoId,
                Titulo = hilo.Titulo,
                AutorId = hilo.AutorId,
                Cerrado = hilo.Cerrado,
                Pinned = hilo.Pinned,
                PinnedOrder = hilo.PinnedOrder,
                LastActivityAt = hilo.LastActivityAt,
                FechaCreacion = hilo.FechaCreacion
            };

            return ResultadoOperacion<HiloDTO>.Exito(dto);
        }

        // ==========================================================
        // FijarHilo(hiloId, pinnedOrder)
        //  - Solo docente/admin
        //  - Pinned=true y PinnedOrder=pinnedOrder
        //  - Ajusta colisiones (mueve hacia abajo órdenes >= pinnedOrder en el mismo foro)
        //  - 404 si no existe, 409 si order inválido (<1)
        // ==========================================================
        public async Task<ResultadoOperacion<HiloDTO>> FijarHiloAsync(int hiloId, int pinnedOrder)
        {
            // Autenticación + rol
            var user = _http.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
            var rolGlobal = user?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<HiloDTO>.Fallo("Usuario no autenticado.", 401);

            if (pinnedOrder < 1)
                return ResultadoOperacion<HiloDTO>.Fallo("Orden de fijado inválido.", 409);

            var hilo = await _context.Hilos.Include(h => h.Autor)
                                           .Include(h => h.Foro).ThenInclude(f => f.Modulo)
                                           .FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<HiloDTO>.Fallo("Hilo no encontrado.", 404);

            // Solo docente del curso o admin global
            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == hilo.Foro.Modulo.CursoId
                                                                     && cu.UsuarioId == userId
                                                                     && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rolGlobal == "Administrador");
            if (!esDocente && !esAdmin)
                return ResultadoOperacion<HiloDTO>.Fallo("No autorizado.", 403);

            // Ajustar colisiones dentro del mismo foro (hilos pinned con order >= pinnedOrder)
            var colisionados = await _context.Hilos
                .Where(h => h.ForoId == hilo.ForoId && h.Pinned && h.PinnedOrder >= pinnedOrder)
                .OrderBy(h => h.PinnedOrder)
                .ToListAsync();

            // Desplazar hacia abajo (order +1) — mantener estabilidad del índice { ForoId, Pinned, PinnedOrder, LastActivityAt }.
            // (El índice compuesto soporta ordenar por Pinned desc, PinnedOrder asc, LastActivityAt desc) :contentReference[oaicite:4]{index=4}
            foreach (var h in colisionados)
            {
                h.PinnedOrder = (h.PinnedOrder ?? 0) + 1;
            }

            hilo.Pinned = true;
            hilo.PinnedOrder = pinnedOrder;

            await _context.SaveChangesAsync();

            var dto = ToDto(hilo);
            return ResultadoOperacion<HiloDTO>.Exito(dto);
        }

        // ==========================================================
        // QuitarFijado(hiloId)
        //  - Pinned=false, PinnedOrder=null
        // ==========================================================
        public async Task<ResultadoOperacion<HiloDTO>> QuitarFijadoAsync(int hiloId)
        {
            var user = _http.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
            var rolGlobal = user?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<HiloDTO>.Fallo("Usuario no autenticado.", 401);

            var hilo = await _context.Hilos.Include(h => h.Autor)
                                           .Include(h => h.Foro).ThenInclude(f => f.Modulo)
                                           .FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<HiloDTO>.Fallo("Hilo no encontrado.", 404);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == hilo.Foro.Modulo.CursoId
                                                                     && cu.UsuarioId == userId
                                                                     && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rolGlobal == "Administrador");
            if (!esDocente && !esAdmin)
                return ResultadoOperacion<HiloDTO>.Fallo("No autorizado.", 403);

            hilo.Pinned = false;
            hilo.PinnedOrder = null;

            await _context.SaveChangesAsync();

            return ResultadoOperacion<HiloDTO>.Exito(ToDto(hilo));
        }

        // ==========================================================
        // CerrarHilo(hiloId, cerrado=true/false)
        //  - Solo docente/admin
        //  - Si cerrado=true, bloquear nuevos posts (la app lo debe respetar)
        // ==========================================================
        public async Task<ResultadoOperacion<HiloDTO>> CerrarHiloAsync(int hiloId, bool cerrado)
        {
            var user = _http.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
            var rolGlobal = user?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<HiloDTO>.Fallo("Usuario no autenticado.", 401);

            var hilo = await _context.Hilos.Include(h => h.Autor)
                                           .Include(h => h.Foro).ThenInclude(f => f.Modulo)
                                           .FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<HiloDTO>.Fallo("Hilo no encontrado.", 404);

            var esDocente = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == hilo.Foro.Modulo.CursoId
                                                                     && cu.UsuarioId == userId
                                                                     && cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rolGlobal == "Administrador");
            if (!esDocente && !esAdmin)
                return ResultadoOperacion<HiloDTO>.Fallo("No autorizado.", 403);

            hilo.Cerrado = cerrado;
            // Si quieres, también podrías setear LockAt/UnlockAt aquí según "cerrado"
            await _context.SaveChangesAsync();

            return ResultadoOperacion<HiloDTO>.Exito(ToDto(hilo));
        }

        // ==========================================================
        // ListarHilos(foroId, filtros, page, size)
        // Orden: Pinned DESC, PinnedOrder ASC, LastActivityAt DESC (Canvas-like)
        // Índice: { ForoId, Pinned, PinnedOrder, LastActivityAt }  :contentReference[oaicite:5]{index=5}
        // ==========================================================
        public async Task<ResultadoOperacion<Page<HiloListItemDTO>>> ListarHilosAsync(
            int foroId, HiloListadoDTO filtros)
        {
            var user = _http.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
            var rolGlobal = user?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userId)) return ResultadoOperacion<Page<HiloListItemDTO>>.Fallo("Usuario no autenticado.", 401);

            // Validar foro y autorización de lectura (miembro del curso o admin)
            var foro = await _context.Foros.AsNoTracking().Include(f => f.Modulo)
                                           .FirstOrDefaultAsync(f => f.ForoId == foroId);
            if (foro == null)
                return ResultadoOperacion<Page<HiloListItemDTO>>.Fallo("Foro no encontrado.", 404);

            var esMiembro = await _context.CursoUsuarios.AnyAsync(cu => cu.CursoId == foro.Modulo.CursoId && cu.UsuarioId == userId);
            var esAdmin = (rolGlobal == "Administrador");
            if (!esMiembro && !esAdmin)
                return ResultadoOperacion<Page<HiloListItemDTO>>.Fallo("No autorizado.", 403);

            var page = filtros.Pagina < 1 ? 1 : filtros.Pagina;
            var size = filtros.CantidadPorPagina < 1 ? 20 : (filtros.CantidadPorPagina > 50 ? 50 : filtros.CantidadPorPagina);

            var query = _context.Hilos.AsNoTracking()
                .Include(h => h.Autor)
                .Where(h => h.ForoId == foroId);

            if (filtros.Pinned.HasValue)
                query = query.Where(h => h.Pinned == filtros.Pinned.Value);

            if (filtros.Cerrado.HasValue)
                query = query.Where(h => h.Cerrado == filtros.Cerrado.Value);

            if (!string.IsNullOrWhiteSpace(filtros.Q))
            {
                var ql = filtros.Q.Trim().ToLower();
                query = query.Where(h => h.Titulo.ToLower().Contains(ql));
            }

            // Orden Canvas-like soportado por el índice compuesto
            query = query
                .OrderByDescending(h => h.Pinned)
                .ThenBy(h => h.PinnedOrder)
                .ThenByDescending(h => h.LastActivityAt);

            var total = await query.CountAsync();
            var registros = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            var items = registros.Select(ToListItem).ToList();

            var pag = new Page<HiloListItemDTO>
            {
                PageNumber = page,
                PageSize = size,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)size),
                Items = items
            };

            return ResultadoOperacion<Page<HiloListItemDTO>>.Exito(pag);
        }

        // ========================
        // Mapeos internos de DTOs
        // ========================
        private static HiloDTO ToDto(Hilo h) => new HiloDTO
        {
            HiloId = h.HiloId,
            ForoId = h.ForoId,
            Titulo = h.Titulo,
            AutorId = h.AutorId ?? string.Empty,
            AutorNombre = h.Autor?.Nombre,
            Cerrado = h.Cerrado,
            Pinned = h.Pinned,
            PinnedOrder = h.PinnedOrder,
            LastActivityAt = h.LastActivityAt,
            FechaCreacion = h.FechaCreacion
        };

        private static HiloListItemDTO ToListItem(Hilo h) => new HiloListItemDTO
        {
            HiloId = h.HiloId,
            ForoId = h.ForoId,
            Titulo = h.Titulo,
            Cerrado = h.Cerrado,
            Pinned = h.Pinned,
            PinnedOrder = h.PinnedOrder,
            LastActivityAt = h.LastActivityAt,
            FechaCreacion = h.FechaCreacion,
            AutorId = h.AutorId ?? string.Empty,
            AutorNombre = h.Autor?.Nombre
        };
    }
}
