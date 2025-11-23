// Método que obtiene todos los módulos asociados a un curso específico.
// 1. Verifica si el curso con el ID proporcionado existe. Si no, retorna un error 404.
// 2. Obtiene los módulos filtrando por CursoId.
// 3. Convierte los módulos a ModuloDTO.
// 4. Retorna la lista de ModuloDTO como una operación exitosa, incluso si la lista está vacía.

using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using LMSBackend.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LMSBackend.API.Services
{
    public class ModuloService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ModuloService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultadoOperacion<List<ModuloDTO>>> ObtenerModulosPorCursoIdAsync(int cursoId)
        {
            var curso = await _context.Cursos.FindAsync(cursoId);
            if (curso == null)
            {
                return ResultadoOperacion<List<ModuloDTO>>.Fallo(mensaje: "Curso no encontrado.", 404);
            }

            var modulos = await _context.Modulos
                .Where(m => m.CursoId == cursoId)
                .ToListAsync();

            var moduloDtos = modulos.Select(m => new ModuloDTO
            {
                ModuloId = m.ModuloId,
                Nombre = m.Nombre,
                CursoId = m.CursoId
            }).ToList();

            return ResultadoOperacion<List<ModuloDTO>>.Exito(moduloDtos);
        }
        // Crea un nuevo módulo para un curso, asegurando que el índice sea positivo y único dentro del curso.
        public async Task<ResultadoOperacion<ModuloDTO>> CrearModuloAsync(ModuloCreacionDTO dto)
        {
            // Verifica si el curso existe
            var curso = await _context.Cursos.FindAsync(dto.CursoId);
            if (curso == null)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("Curso no encontrado.", 404);
            }

            // Valida que el índice sea positivo
            if (dto.Indice < 0)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("El índice debe ser un número positivo.", 400);
            }

            // Verifica que el índice sea único dentro del curso
            var existeIndice = await _context.Modulos
                .AnyAsync(m => m.CursoId == dto.CursoId && m.Indice == dto.Indice);
            if (existeIndice)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("El índice ya está en uso para este curso.", 400);
            }

            // Crea el nuevo módulo
            var modulo = new Modulo
            {
                Nombre = dto.Nombre,
                CursoId = dto.CursoId,
                Indice = dto.Indice
            };

            _context.Modulos.Add(modulo);
            await _context.SaveChangesAsync();

            var moduloDto = new ModuloDTO
            {
                ModuloId = modulo.ModuloId,
                Nombre = modulo.Nombre,
                CursoId = modulo.CursoId,
                Indice = modulo.Indice
            };

            return ResultadoOperacion<ModuloDTO>.Exito(moduloDto);
        }
        // Edita un módulo existente validando que el índice siga siendo único dentro del curso.
        public async Task<ResultadoOperacion<ModuloDTO>> EditarModuloAsync(int moduloId, ModuloEdicionDTO dto)
        {
            // Busca el módulo por su ID
            var modulo = await _context.Modulos.FindAsync(moduloId);
            if (modulo == null)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("Módulo no encontrado.", 404);
            }

            // Verifica si el curso al que pertenece el módulo existe
            var curso = await _context.Cursos.FindAsync(modulo.CursoId);
            if (curso == null)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("Curso no encontrado.", 404);
            }

            // Valida que el índice sea positivo
            if (dto.Indice < 0)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("El índice debe ser un número positivo.", 400);
            }

            // Verifica que el índice sea único dentro del curso
            var existeIndice = await _context.Modulos
                .AnyAsync(m => m.CursoId == modulo.CursoId && m.Indice == dto.Indice && m.ModuloId != moduloId);
            if (existeIndice)
            {
                return ResultadoOperacion<ModuloDTO>.Fallo("El índice ya está en uso para este curso.", 400);
            }

            // Actualiza los datos del módulo
            modulo.Nombre = dto.Nombre;
            modulo.Indice = dto.Indice;

            _context.Modulos.Update(modulo);
            await _context.SaveChangesAsync();

            var moduloDto = new ModuloDTO
            {
                ModuloId = modulo.ModuloId,
                Nombre = modulo.Nombre,
                CursoId = modulo.CursoId,
                Indice = modulo.Indice
            };

            return ResultadoOperacion<ModuloDTO>.Exito(moduloDto);
        }
        public async Task<ResultadoOperacion<string>> EliminarModuloAsync(int moduloId)
        {
            // Buscar el módulo por ID
            var modulo = await _context.Modulos.FindAsync(moduloId);
            if (modulo == null)
            {
                return ResultadoOperacion<string>.Fallo("Módulo no encontrado.", 404);
            }

            // Verificar si es predeterminado
            if (modulo.EsPredeterminado)
            {
                return ResultadoOperacion<string>.Fallo("No se puede eliminar un módulo predeterminado.", 400);
            }

            // Eliminar el módulo
            _context.Modulos.Remove(modulo);
            await _context.SaveChangesAsync();

            return ResultadoOperacion<string>.Exito("Módulo eliminado correctamente.");
        }
        public async Task<ResultadoOperacion<List<ModuloDTO>>> ObtenerModulosPorCursoIdParaUsuarioAsync(int cursoId)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(usuarioId))
                return ResultadoOperacion<List<ModuloDTO>>.Fallo("Usuario no autenticado.", 401);

            var cursoExiste = await _context.Cursos.AnyAsync(c => c.Nrc == cursoId);
            if (!cursoExiste)
                return ResultadoOperacion<List<ModuloDTO>>.Fallo("Curso no encontrado.", 404);

            var estaAsignado = await _context.CursoUsuarios
                .AnyAsync(cu => cu.CursoId == cursoId && cu.UsuarioId == usuarioId);

            if (!estaAsignado)
                return ResultadoOperacion<List<ModuloDTO>>.Fallo("No tienes acceso a este curso.", 403);

            var modulos = await _context.Modulos
                .Where(m => m.CursoId == cursoId)
                .OrderBy(m => m.Indice)
                .Select(m => new ModuloDTO
                {
                    ModuloId = m.ModuloId,
                    Nombre = m.Nombre,
                    Indice = m.Indice,
                    CursoId = m.CursoId
                })
                .ToListAsync();

            return ResultadoOperacion<List<ModuloDTO>>.Exito(modulos);
        }
        
    }
}
