using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMSBackend.API.Services
{
    public class PostService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;

        public PostService(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        // ==========================================================
        // CrearPost(hiloId, autorId, contenido)
        // - 404 si hilo no existe
        // - 409 si hilo.Cerrado o LockAt vencido
        // - Crea Post: SoftDeleted=false, Editado=false, FechaCreacion=UtcNow
        // - Actualiza Hilo.LastActivityAt=UtcNow
        // ==========================================================
        public async Task<ResultadoOperacion<PostDTO>> CrearPostAsync(int hiloId, string autorId, string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido) || contenido.Trim().Length > 10_000)
                return ResultadoOperacion<PostDTO>.Fallo("Contenido inválido (vacío o > 10000).", 400);

            var hilo = await _context.Hilos
                .Include(h => h.Foro)
                .ThenInclude(f => f.Modulo)
                .FirstOrDefaultAsync(h => h.HiloId == hiloId);

            if (hilo == null)
                return ResultadoOperacion<PostDTO>.Fallo("Hilo no encontrado.", 404);

            if (hilo.Foro?.Modulo == null)
                return ResultadoOperacion<PostDTO>.Fallo("El foro no está asociado a un módulo válido.", 500);

            // Reglas de cierre/bloqueo de hilo
            var ahora = DateTime.UtcNow;
            var bloqueadoPorTiempo =
                hilo.LockAt.HasValue && ahora >= hilo.LockAt.Value &&
                (!hilo.UnlockAt.HasValue || ahora < hilo.UnlockAt.Value); // ventana activa de lock
            if (hilo.Cerrado || bloqueadoPorTiempo)
                return ResultadoOperacion<PostDTO>.Fallo("El hilo está cerrado o bloqueado.", 409); // :contentReference[oaicite:5]{index=5}

            // (Sugerido) Debe pertenecer al curso del foro
            var pertenece = await _context.CursoUsuarios
                .AsNoTracking()
                .AnyAsync(cu => cu.CursoId == hilo.Foro.Modulo.CursoId && cu.UsuarioId == autorId);
            if (!pertenece)
                return ResultadoOperacion<PostDTO>.Fallo("No perteneces al curso de este hilo.", 403);

            var post = new Post
            {
                HiloId = hiloId,
                AutorId = autorId,
                Contenido = contenido.Trim(),
                Editado = false,
                SoftDeleted = false,
                FechaCreacion = ahora
            };

            _context.Posts.Add(post);
            hilo.LastActivityAt = ahora; // mantener orden por actividad reciente del hilo
            await _context.SaveChangesAsync();

            return ResultadoOperacion<PostDTO>.Exito(ToDto(post));
        }

        // ==========================================================
        // EditarPost(postId, autorId, contenido)
        // - Solo autor o moderador (docente del curso o admin)
        // - Set Editado=true, EditedAt=UtcNow
        // ==========================================================
        public async Task<ResultadoOperacion<PostDTO>> EditarPostAsync(int postId, string autorId, string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido) || contenido.Trim().Length > 10_000)
                return ResultadoOperacion<PostDTO>.Fallo("Contenido inválido (vacío o > 10000).", 400);

            var postResult = await _context.Posts
                .Include(p => p.Autor)
                .Include(p => p.Hilo)
                .ThenInclude(h => h.Foro)
                .ThenInclude(f => f.Modulo)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (postResult is not Post post)
                return ResultadoOperacion<PostDTO>.Fallo("Post no encontrado.", 404);

            if (!TryGetModuloFromPost(post, out var modulo))
                return ResultadoOperacion<PostDTO>.Fallo("El foro del post no está asociado a un módulo válido.", 500);

            // Autorización: autor, docente del curso o admin global
            var rolGlobal = _http.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            var esDocente = await _context.CursoUsuarios.AnyAsync(cu =>
                cu.CursoId == modulo.CursoId &&
                cu.UsuarioId == autorId &&
                cu.RolEnCurso == RolEnCurso.Docente);

            var esAdmin = (rolGlobal == "Administrador");
            var esAutor = post.AutorId == autorId;
            if (!esAutor && !esDocente && !esAdmin)
                return ResultadoOperacion<PostDTO>.Fallo("No autorizado.", 403);

            post.Contenido = contenido.Trim();
            post.Editado = true;
            post.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ResultadoOperacion<PostDTO>.Exito(ToDto(post));
        }

        // ==========================================================
        // BorrarPost(postId, autorId)
        // - Soft delete: SoftDeleted=true (contenido se mantiene; placeholder lo aplica el render)
        // - Mantiene orden cronológico intacto
        // ==========================================================
        public async Task<ResultadoOperacion<PostDTO>> BorrarPostAsync(int postId, string autorId)
        {
            var postResult = await _context.Posts
                .Include(p => p.Autor)
                .Include(p => p.Hilo)
                .ThenInclude(h => h.Foro)
                .ThenInclude(f => f.Modulo)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (postResult is not Post post)
                return ResultadoOperacion<PostDTO>.Fallo("Post no encontrado.", 404);

            if (!TryGetModuloFromPost(post, out var modulo))
                return ResultadoOperacion<PostDTO>.Fallo("El foro del post no está asociado a un módulo válido.", 500);

            var rolGlobal = _http.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            var esDocente = await _context.CursoUsuarios.AnyAsync(cu =>
                cu.CursoId == modulo.CursoId &&
                cu.UsuarioId == autorId &&
                cu.RolEnCurso == RolEnCurso.Docente);
            var esAdmin = (rolGlobal == "Administrador");
            var esAutor = post.AutorId == autorId;

            if (!esAutor && !esDocente && !esAdmin)
                return ResultadoOperacion<PostDTO>.Fallo("No autorizado.", 403);

            post.SoftDeleted = true; // el render decide si muestra placeholder
            await _context.SaveChangesAsync();

            return ResultadoOperacion<PostDTO>.Exito(ToDto(post));
        }

        // ==========================================================
        // ListarPosts(hiloId, page, size, viewerId)
        // - Orden cronológico: FechaCreacion ASC por hilo (índice compuesto)
        // - Si foro.RequireInitialPostToView == true y viewer no ha posteado:
        //     devolver solo el post inicial (y metadatos) hasta que publique.
        // ==========================================================
        public async Task<ResultadoOperacion<Page<PostDTO>>> ListarPostsAsync(
            int hiloId, int page, int size, string viewerId)
        {
            if (page < 1 || size < 1 || size > 100)
                return ResultadoOperacion<Page<PostDTO>>.Fallo("Parámetros de paginación inválidos.", 400);

            var hilo = await _context.Hilos
                .AsNoTracking()
                .Include(h => h.Foro)
                .ThenInclude(f => f.Modulo)
                .FirstOrDefaultAsync(h => h.HiloId == hiloId);

            if (hilo == null)
                return ResultadoOperacion<Page<PostDTO>>.Fallo("Hilo no encontrado.", 404);

            if (hilo.Foro?.Modulo == null)
                return ResultadoOperacion<Page<PostDTO>>.Fallo("El foro no está asociado a un módulo válido.", 500);

            // Autorización de lectura: miembro del curso o admin
            var rolGlobal = _http.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            var esMiembro = await _context.CursoUsuarios.AnyAsync(cu =>
                cu.CursoId == hilo.Foro.Modulo.CursoId && cu.UsuarioId == viewerId);
            var esAdmin = (rolGlobal == "Administrador");
            if (!esMiembro && !esAdmin)
                return ResultadoOperacion<Page<PostDTO>>.Fallo("No autorizado.", 403);

            // Gating por RequireInitialPostToView
            if (hilo.Foro.RequireInitialPostToView && !esAdmin)
            {
                var viewerHaPosteado = await _context.Posts
                    .AsNoTracking()
                    .AnyAsync(p => p.HiloId == hiloId && p.AutorId == viewerId);

                if (!viewerHaPosteado)
                {
                    var primero = await _context.Posts
                        .AsNoTracking()
                        .Where(p => p.HiloId == hiloId)
                        .OrderBy(p => p.FechaCreacion) // usa índice {HiloId, FechaCreacion} :contentReference[oaicite:6]{index=6}
                        .FirstOrDefaultAsync();

                    var pageSoloPrimero = new Page<PostDTO>
                    {
                        PageNumber = 1,
                        PageSize = 1,
                        TotalItems = primero == null ? 0 : 1,
                        TotalPages = primero == null ? 0 : 1,
                        Items = primero == null ? new() : new() { ToDto(primero) }
                    };
                    return ResultadoOperacion<Page<PostDTO>>.Exito(pageSoloPrimero);
                }
            }

            // Listado normal (cronológico ascendente)
            var query = _context.Posts.AsNoTracking()
                .Include(p => p.Autor)
                .Where(p => p.HiloId == hiloId)
                .OrderBy(p => p.FechaCreacion); // índice soporta este orden :contentReference[oaicite:7]{index=7}

            var total = await query.CountAsync();
            var posts = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            var pag = new Page<PostDTO>
            {
                PageNumber = page,
                PageSize = size,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)size),
                Items = posts.Select(ToDto).ToList()
            };

            return ResultadoOperacion<Page<PostDTO>>.Exito(pag);
        }

        // ========================
        // Mapeo DTO
        // ========================
        private static PostDTO ToDto(Post p) => new PostDTO
        {
            PostId = p.PostId,
            HiloId = p.HiloId,
            AutorId = p.AutorId ?? string.Empty,
            AutorNombre = p.Autor?.Nombre,
            Contenido = p.Contenido ?? string.Empty,
            Editado = p.Editado,
            EditedAt = p.EditedAt,
            SoftDeleted = p.SoftDeleted,
            FechaCreacion = p.FechaCreacion
        };

	private static bool TryGetModuloFromPost(Post post, out Modulo modulo)
    {
        var hilo = post.Hilo;
        if (hilo == null)
        {
            modulo = null!;
            return false;
        }

        var foro = hilo.Foro;
        if (foro == null)
        {
            modulo = null!;
            return false;
        }

        modulo = foro.Modulo;
        return modulo != null;
    }
}
}
