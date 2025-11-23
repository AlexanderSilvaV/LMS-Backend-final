
using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMSBackend.API.Services
{
    public class LecturaService
    {
        private readonly ApplicationDbContext _context;

        public LecturaService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // MarcarComoLeido(hiloId, usuarioId)
        // Upsert en HiloLectura: LastReadAt = UtcNow
        // 404 si el hilo no existe
        // =========================================
        public async Task<ResultadoOperacion<DateTime>> MarcarComoLeidoAsync(int hiloId, string usuarioId)
        {
            var hilo = await _context.Hilos.AsNoTracking().FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<DateTime>.Fallo("Hilo no encontrado.", 404);

            var ahora = DateTime.UtcNow;

            var lectura = await _context.HiloLecturas
                .FirstOrDefaultAsync(l => l.HiloId == hiloId && l.UsuarioId == usuarioId);

            if (lectura == null)
            {
                lectura = new HiloLectura
                {
                    HiloId = hiloId,
                    UsuarioId = usuarioId,
                    LastReadAt = ahora
                };
                _context.HiloLecturas.Add(lectura);
            }
            else
            {
                lectura.LastReadAt = ahora;
            }

            await _context.SaveChangesAsync();
            return ResultadoOperacion<DateTime>.Exito(ahora, "Marcado como leído.");
        }

        // =========================================
        // UnreadCount(hiloId, usuarioId)
        // COUNT(posts WHERE FechaCreacion > LastReadAt AND SoftDeleted=false)
        // Si no hay registro de lectura, cuenta todos los no-borrados
        // =========================================
        public async Task<ResultadoOperacion<int>> UnreadCountAsync(int hiloId, string usuarioId)
        {
            var hilo = await _context.Hilos.AsNoTracking().FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<int>.Fallo("Hilo no encontrado.", 404);

            var lastRead = await _context.HiloLecturas
                .AsNoTracking()
                .Where(l => l.HiloId == hiloId && l.UsuarioId == usuarioId)
                .Select(l => (DateTime?)l.LastReadAt)
                .FirstOrDefaultAsync();

            IQueryable<Post> baseQuery = _context.Posts.AsNoTracking()
                .Where(p => p.HiloId == hiloId && !p.SoftDeleted);

            var count = lastRead.HasValue
                ? await baseQuery.CountAsync(p => p.FechaCreacion > lastRead.Value)
                : await baseQuery.CountAsync();

            return ResultadoOperacion<int>.Exito(count);
        }

        // =========================================
        // MarcarHastaPost(hiloId, postId, usuarioId) (opcional)
        // LastReadAt = FechaCreacion(postId)
        // 404 si post no existe o no pertenece al hilo
        // =========================================
        public async Task<ResultadoOperacion<DateTime>> MarcarHastaPostAsync(int hiloId, int postId, string usuarioId)
        {
            var post = await _context.Posts
                .Include(p => p.Hilo)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null || post.HiloId != hiloId)
                return ResultadoOperacion<DateTime>.Fallo("Post no encontrado en el hilo.", 404);

            var lectura = await _context.HiloLecturas
                .FirstOrDefaultAsync(l => l.HiloId == hiloId && l.UsuarioId == usuarioId);

            var hasta = post.FechaCreacion;

            if (lectura == null)
            {
                lectura = new HiloLectura
                {
                    HiloId = hiloId,
                    UsuarioId = usuarioId,
                    LastReadAt = hasta
                };
                _context.HiloLecturas.Add(lectura);
            }
            else
            {
                // Solo avanzar hacia adelante (opcional). Si quieres permitir retroceso, quita el if.
                if (hasta > lectura.LastReadAt)
                    lectura.LastReadAt = hasta;
            }

            await _context.SaveChangesAsync();
            return ResultadoOperacion<DateTime>.Exito(hasta, "Leído hasta el post indicado.");
        }
    }
}
