
using LMSBackend.API.Data;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMSBackend.API.Services
{
    public class SuscripcionService
    {
        private readonly ApplicationDbContext _context;

        public SuscripcionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // Suscribirse(hiloId, usuarioId)
        // - 404 si hilo no existe
        // - Idempotente: si ya existe, 200 sin cambios
        // =========================================
        public async Task<ResultadoOperacion<string>> SuscribirseAsync(int hiloId, string usuarioId)
        {
            // Hilo existe
            var hilo = await _context.Hilos.AsNoTracking().FirstOrDefaultAsync(h => h.HiloId == hiloId);
            if (hilo == null)
                return ResultadoOperacion<string>.Fallo("Hilo no encontrado.", 404);

            // Ya suscrito (PK compuesta evita duplicado)
            var existe = await _context.HiloSuscripciones
                .AsNoTracking()
                .AnyAsync(s => s.HiloId == hiloId && s.UsuarioId == usuarioId);

            if (existe)
                return ResultadoOperacion<string>.Exito("Ya estabas suscrito a este hilo.");

            _context.HiloSuscripciones.Add(new HiloSuscripcion
            {
                HiloId = hiloId,
                UsuarioId = usuarioId,
                FechaSuscripcion = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return ResultadoOperacion<string>.Exito("Suscripción creada.");
        }

        // =========================================
        // Desuscribirse(hiloId, usuarioId)
        // - Idempotente: si no existe, 200 sin cambios
        // =========================================
        public async Task<ResultadoOperacion<string>> DesuscribirseAsync(int hiloId, string usuarioId)
        {
            var sub = await _context.HiloSuscripciones
                .FirstOrDefaultAsync(s => s.HiloId == hiloId && s.UsuarioId == usuarioId);

            if (sub != null)
            {
                _context.HiloSuscripciones.Remove(sub);
                await _context.SaveChangesAsync();
                return ResultadoOperacion<string>.Exito("Suscripción eliminada.");
            }

            return ResultadoOperacion<string>.Exito("No estabas suscrito (sin cambios).");
        }

        // =========================================
        // MisSuscripciones(usuarioId, page, size)
        // JOIN con Hilos y Foros para exponer título y actividad
        // =========================================
        public async Task<ResultadoOperacion<Page<SuscripcionListItemDTO>>> MisSuscripcionesAsync(
            string usuarioId, int page = 1, int size = 20)
        {
            if (page < 1 || size < 1 || size > 50)
                return ResultadoOperacion<Page<SuscripcionListItemDTO>>.Fallo("Parámetros de paginación inválidos.", 400);

            // JOIN s (subs) -> h (hilos) -> f (foros)
            var query =
                from s in _context.HiloSuscripciones.AsNoTracking()
                join h in _context.Hilos.AsNoTracking() on s.HiloId equals h.HiloId
                join f in _context.Foros.AsNoTracking() on h.ForoId equals f.ForoId
                where s.UsuarioId == usuarioId
                select new SuscripcionListItemDTO
                {
                    HiloId = h.HiloId,
                    ForoId = h.ForoId,
                    HiloTitulo = h.Titulo,
                    ForoTitulo = f.Titulo,
                    Cerrado = h.Cerrado,
                    Pinned = h.Pinned,
                    PinnedOrder = h.PinnedOrder,
                    LastActivityAt = h.LastActivityAt,
                    FechaSuscripcion = s.FechaSuscripcion
                };

            // Orden más útil: actividad reciente (desc)
            query = query.OrderByDescending(x => x.Pinned)
                         .ThenBy(x => x.PinnedOrder)
                         .ThenByDescending(x => x.LastActivityAt);

            var total = await query.CountAsync();
            var registros = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            var pag = new Page<SuscripcionListItemDTO>
            {
                PageNumber = page,
                PageSize = size,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)size),
                Items = registros
            };

            return ResultadoOperacion<Page<SuscripcionListItemDTO>>.Exito(pag);
        }
    }
}
