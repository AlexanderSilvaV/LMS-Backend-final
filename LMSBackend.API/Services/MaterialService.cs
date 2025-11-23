using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using LMSBackend.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace LMSBackend.API.Services
{
    public class MaterialService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MaterialService(ApplicationDbContext context, IStorageService storageService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _storageService = storageService;
            _httpContextAccessor = httpContextAccessor;
        }

        // Obtener materiales de un módulo
        public async Task<ResultadoOperacion<List<MaterialDTO>>> ObtenerMaterialesPorModuloIdAsync(int moduloId)
        {
            var modulo = await _context.Modulos.FindAsync(moduloId);
            if (modulo == null)
            {
                return ResultadoOperacion<List<MaterialDTO>>.Fallo("Modulo no encontrado", 404);
            }

            var materiales = await _context.Materiales.Where(m => m.ModuloId == moduloId).ToListAsync();

            var materialesDTO = materiales.Select(m => new MaterialDTO
            {
                MaterialId = m.MaterialId,
                Nombre = m.Nombre,
                Ruta = m.Ruta,
                Tipo = m.Tipo.ToString(),
                ModuloId = m.ModuloId
            }).ToList();
            return ResultadoOperacion<List<MaterialDTO>>.Exito(materialesDTO, "Materiales obtenidos con éxito");
        }

        // Crear nuevo material (archivo, enlace o video)
        public async Task<ResultadoOperacion<MaterialDTO>> CrearMaterialAsync(MaterialCreacionDTO dto)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No se pudo obtener el ID del usuario autenticado.", 401);
            }
            var modulo = await _context.Modulos.FindAsync(dto.ModuloId);
            if (modulo == null)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("Módulo no encontrado", 404);
            }
            var estaAsingado = await _context.CursoUsuarios.AnyAsync(cu => cu.UsuarioId == usuarioId && cu.CursoId == modulo.CursoId);
            if (!estaAsingado)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No tienes permisos para gestionar materiales de este módulo.", 403);
            }
            if (string.IsNullOrEmpty(dto.Nombre))
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("El Campo 'Nombre' es obligatorio", 400);
            }
            if (string.IsNullOrWhiteSpace(dto.Ruta))
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("El Campo 'Ruta' es obligatorio.", 400);
            }
            if (dto.Tipo == TipoMaterial.Archivo)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No se permite crear materiales tipo Archivo desde este endpoint. Usa el endpoint de subida de archivos.", 400);
            }
            if (dto.Tipo == TipoMaterial.Enlace || dto.Tipo == TipoMaterial.Video)
            {
                var esUrlValida = Uri.TryCreate(dto.Ruta, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!esUrlValida)
                {
                    return ResultadoOperacion<MaterialDTO>.Fallo("La URL ingresada no es válida.", 400);
                }
            }
            var material = new Material
            {
                Nombre = dto.Nombre,
                Tipo = dto.Tipo,
                Ruta = dto.Ruta,
                ModuloId = dto.ModuloId,
                UsuarioId = usuarioId
            };

            _context.Materiales.Add(material);
            await _context.SaveChangesAsync();

            var materialDTO = new MaterialDTO
            {
                MaterialId = material.MaterialId,
                Nombre = material.Nombre,
                Tipo = material.Tipo.ToString(),
                Ruta = material.Ruta,
                ModuloId = material.ModuloId
            };

            return ResultadoOperacion<MaterialDTO>.Exito(materialDTO, "Material creado con éxito");
        }

        // Editar material
        public async Task<ResultadoOperacion<MaterialDTO>> EditarMaterialAsync(int materialId, MaterialEdicionDTO dto)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
                return ResultadoOperacion<MaterialDTO>.Fallo("No se pudo obtener el ID del usuario autenticado.", 401);
            var material = await _context.Materiales.FindAsync(materialId);
            if (material == null)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("Material no encontrado", 404);
            }
            if (rol != "Administrador" && material.UsuarioId != usuarioId)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No tiene permisos para editar este material", 403);
            }
            var modulo = await _context.Modulos.FindAsync(material.ModuloId);
            if (modulo == null)
                return ResultadoOperacion<MaterialDTO>.Fallo("Módulo no encontrado.", 404);
            var estaAsignado = await _context.CursoUsuarios
                    .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.CursoId == modulo.CursoId);
            if (!estaAsignado)
                return ResultadoOperacion<MaterialDTO>.Fallo("No tienes permisos para editar este material.", 403);

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("El Campo 'Nombre' es obligatorio.", 400);
            }

            // Actualizar nombre, ruta y guardado de los respectivos cambios
            material.Nombre = dto.Nombre;


            if (!string.IsNullOrWhiteSpace(dto.Ruta))
            {
                if (material.Tipo == TipoMaterial.Archivo)
                    return ResultadoOperacion<MaterialDTO>.Fallo("No se puede modificar la ruta de un archivo subido.", 400);
                if (!Uri.TryCreate(dto.Ruta, UriKind.Absolute, out var uri)
                           || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return ResultadoOperacion<MaterialDTO>.Fallo("La nueva URL ingresada no es válida.", 400);
                }
                material.Ruta = dto.Ruta;

            }

            await _context.SaveChangesAsync();

            // Mapeo DTO y devolver DTO
            var materialDTO = new MaterialDTO
            {
                MaterialId = material.MaterialId,
                Nombre = material.Nombre,
                Ruta = material.Ruta,
                Tipo = material.Tipo.ToString(),
                ModuloId = material.ModuloId
            };
            return ResultadoOperacion<MaterialDTO>.Exito(materialDTO, "Material actualizado con éxito");
        }

        // Eliminar material
        public async Task<ResultadoOperacion<string>> EliminarMaterialAsync(int materialId)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
                return ResultadoOperacion<string>.Fallo("No se pudo obtener el ID del usuario autenticado.", 401);

            var rol = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Buscar material por ID
            var material = await _context.Materiales.FindAsync(materialId);
            if (material == null)
            {
                return ResultadoOperacion<string>.Fallo("Material no encontrado", 404);
            }
            var modulo = await _context.Modulos.FindAsync(material.ModuloId);
            if (modulo == null)
                return ResultadoOperacion<string>.Fallo("Módulo no encontrado.", 404);
            var estaAsignado = await _context.CursoUsuarios
                .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.CursoId == modulo.CursoId);
            if (!estaAsignado)
                return ResultadoOperacion<string>.Fallo("No tienes permisos para eliminar este material.", 403);
            if (rol != "Administrador" && material.UsuarioId != usuarioId)
            {
                return ResultadoOperacion<string>.Fallo("No tiene permisos para eliminar este material.", 403);
            }
            if (material.Tipo == TipoMaterial.Archivo && !string.IsNullOrEmpty(material.Ruta))
            {
                await _storageService.DeleteFileAsync(Path.GetFileName(material.Ruta), "materials");
            }

            // Eliminar material de la base de datos
            _context.Materiales.Remove(material);
            await _context.SaveChangesAsync();



            return ResultadoOperacion<string>.Exito("Material eliminado con éxito");
        }
        public async Task<ResultadoOperacion<MaterialDTO>> CrearMaterialArchivoAsync(int moduloId, IFormFile archivo)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioId))
                return ResultadoOperacion<MaterialDTO>.Fallo("No se pudo obtener el ID del usuario autenticado.", 401);

            var modulo = await _context.Modulos.FindAsync(moduloId);
            if (modulo == null)
                return ResultadoOperacion<MaterialDTO>.Fallo("Módulo no encontrado", 404);
            var estaAsignado = await _context.CursoUsuarios
                .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.CursoId == modulo.CursoId);
            if (!estaAsignado)
                return ResultadoOperacion<MaterialDTO>.Fallo("No tienes permisos para gestionar materiales de este módulo.", 403);
            if (archivo == null || archivo.Length == 0)
                return ResultadoOperacion<MaterialDTO>.Fallo("El archivo está vacío", 400);

            var nombreFinal = $"{Guid.NewGuid()}_{archivo.FileName}";

            try
            {
                var (fileId, fileUrl) = await _storageService.SaveFileAsync(usuarioId, archivo, "materials");

                var material = new Material
                {
                    Nombre = archivo.FileName,
                    Tipo = TipoMaterial.Archivo,
                    Ruta = fileUrl,
                    ModuloId = moduloId,
                    UsuarioId = usuarioId
                };

                _context.Materiales.Add(material);
                await _context.SaveChangesAsync();

                var dto = new MaterialDTO
                {
                    MaterialId = material.MaterialId,
                    Nombre = material.Nombre,
                    Tipo = material.Tipo.ToString(),
                    Ruta = material.Ruta,
                    ModuloId = material.ModuloId
                };

                return ResultadoOperacion<MaterialDTO>.Exito(dto, "Material subido con éxito");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo($"Error al subir archivo: {ex.Message}", 500);
            }
        }
        public async Task<ResultadoOperacion<MaterialDTO>> ReemplazarArchivoAsync(int materialId, IFormFile? nuevoArchivo)
        {
            var usuarioId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(usuarioId))
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No se pudo obtener el ID del usuario autenticado.", 401);
            }

            if (nuevoArchivo == null || nuevoArchivo.Length == 0)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("El nuevo archivo está vacío.", 400);
            }

            var material = await _context.Materiales.FindAsync(materialId);
            if (material == null)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("Material no encontrado.", 404);
            }
            var modulo = await _context.Modulos.FindAsync(material.ModuloId);
            if (modulo == null)
                return ResultadoOperacion<MaterialDTO>.Fallo("Módulo no encontrado.", 404);
            var estaAsignado = await _context.CursoUsuarios
                .AnyAsync(cu => cu.UsuarioId == usuarioId && cu.CursoId == modulo.CursoId);
            if (!estaAsignado)
                return ResultadoOperacion<MaterialDTO>.Fallo("No tienes permisos para reemplazar este archivo.", 403);
            if (material.Tipo != TipoMaterial.Archivo)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("Este material no es un archivo. Solo archivos pueden ser reemplazados.", 400);
            }

            if (rol != "Administrador" && material.UsuarioId != usuarioId)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo("No tiene permisos para reemplazar este material.", 403);
            }

            var nombreFinal = $"{Guid.NewGuid()}_{nuevoArchivo.FileName}";

            try
            {
                // Subir nuevo archivo
                var (nuevoFileId, nuevoFileUrl) = await _storageService.SaveFileAsync(usuarioId, nuevoArchivo, "materials");

                if (!string.IsNullOrEmpty(material.Ruta))
                {
                    await _storageService.DeleteFileAsync(Path.GetFileName(material.Ruta), "materials");
                }

                // Actualizar los datos del material
                material.Ruta = nuevoFileUrl;
                material.Nombre = nuevoArchivo.FileName;

                await _context.SaveChangesAsync();

                var dto = new MaterialDTO
                {
                    MaterialId = material.MaterialId,
                    Nombre = material.Nombre,
                    Ruta = material.Ruta,
                    Tipo = material.Tipo.ToString(),
                    ModuloId = material.ModuloId
                };

                return ResultadoOperacion<MaterialDTO>.Exito(dto, "Archivo reemplazado exitosamente.");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<MaterialDTO>.Fallo($"Error al reemplazar el archivo: {ex.Message}", 500);
            }
        }

        // Generar URL de descarga fresca para un material
        public async Task<ResultadoOperacion<string>> GenerarUrlDescargaAsync(int materialId)
        {
            var material = await _context.Materiales.FindAsync(materialId);
            if (material == null)
            {
                return ResultadoOperacion<string>.Fallo("Material no encontrado", 404);
            }

            if (material.Tipo != TipoMaterial.Archivo)
            {
                return ResultadoOperacion<string>.Fallo("Este material no es un archivo descargable", 400);
            }

            if (string.IsNullOrEmpty(material.Ruta))
            {
                return ResultadoOperacion<string>.Fallo("El material no tiene una ruta válida", 404);
            }

            try
            {
                // Extraer el fileId de la URL de S3
                // Formato esperado: https://bucket.s3.region.amazonaws.com/uploads/userId/subfolder/fileId?params
                var uri = new Uri(material.Ruta);
                var path = uri.AbsolutePath; // /uploads/userId/subfolder/fileId
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length < 4)
                {
                    return ResultadoOperacion<string>.Fallo("Formato de URL de S3 inválido", 500);
                }

                // Extraer componentes: uploads/userId/subfolder/fileId
                var userId = segments[1];
                var subfolder = segments[2];
                var fileId = segments[3];

                // Generar nueva URL prefirmada
                var downloadUrlResponse = await _storageService.GeneratePresignedDownloadUrlAsync(userId, new GenerateDownloadUrlRequestDTO
                {
                    FileId = fileId,
                    Subfolder = subfolder,
                    DownloadFileName = material.Nombre
                });

                return ResultadoOperacion<string>.Exito(downloadUrlResponse.PresignedUrl, "URL de descarga generada con éxito");
            }
            catch (UriFormatException)
            {
                return ResultadoOperacion<string>.Fallo("La URL del material no es válida", 500);
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<string>.Fallo($"Error al generar URL de descarga: {ex.Message}", 500);
            }
        }

    }
}
