using Microsoft.AspNetCore.Http;
using LMSBackend.API.Validators;
using LMSBackend.API.DTOs;

namespace LMSBackend.API.Services
{
    /// <summary>
    /// Implementación de IStorageService que almacena archivos localmente en el servidor
    /// </summary>
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalStorageService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _uploadsPath;

        public LocalStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalStorageService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            // Crear directorio de uploads si no existe
            _uploadsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
                _logger.LogInformation("Directorio de uploads creado: {UploadsPath}", _uploadsPath);
            }

            _logger.LogInformation("LocalStorageService inicializado - Ruta: {UploadsPath}", _uploadsPath);
        }

        /// <summary>
        /// Guarda un archivo en el almacenamiento local
        /// </summary>
        public async Task<(string fileId, string fileUrl)> SaveFileAsync(string userId, IFormFile archivo, string subfolder)
        {
            try
            {
                // 1. Validar archivo
                var (isValid, errorMessage) = FileValidator.ValidateFile(
                    archivo.FileName,
                    archivo.ContentType,
                    archivo.Length);

                if (!isValid)
                {
                    throw new ArgumentException(errorMessage);
                }

                // 2. Sanitizar nombre del archivo
                var sanitizedFileName = FileValidator.SanitizeFileName(archivo.FileName);
                var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();

                // 3. Generar ID único para el archivo
                var fileId = $"{Guid.NewGuid()}{extension}";

                // 4. Construir la ruta completa del archivo
                var relativePath = BuildRelativePath(userId, fileId, subfolder);
                var fullPath = Path.Combine(_uploadsPath, relativePath);

                // 5. Crear directorios si no existen
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 6. Guardar el archivo
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                _logger.LogInformation("Archivo guardado localmente: {FilePath}", fullPath);

                // 7. Generar URL accesible
                var fileUrl = GenerateFileUrl(relativePath);

                return (fileId, fileUrl);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al guardar archivo localmente");
                throw new Exception($"Error al guardar archivo: {e.Message}", e);
            }
        }

        /// <summary>
        /// Elimina un archivo del almacenamiento local
        /// </summary>
        public async Task DeleteFileAsync(string fileId, string subfolder)
        {
            try
            {
                // Construir ruta del archivo (sin userId porque no lo tenemos en la interfaz)
                var relativePath = string.IsNullOrEmpty(subfolder)
                    ? fileId
                    : Path.Combine(subfolder, fileId);

                var fullPath = Path.Combine(_uploadsPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Archivo eliminado: {FilePath}", fullPath);
                }
                else
                {
                    _logger.LogWarning("Intento de eliminar archivo inexistente: {FilePath}", fullPath);
                }

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al eliminar archivo localmente");
                throw new Exception($"Error al eliminar archivo: {e.Message}", e);
            }
        }

        /// <summary>
        /// Copia un archivo existente creando una nueva copia con ID único
        /// </summary>
        public async Task<(string fileId, string fileUrl)> CopyFileAsync(string originalFileId, string subfolder)
        {
            try
            {
                // Construir ruta del archivo original
                var originalRelativePath = string.IsNullOrEmpty(subfolder)
                    ? originalFileId
                    : Path.Combine(subfolder, originalFileId);

                var originalFullPath = Path.Combine(_uploadsPath, originalRelativePath);

                if (!File.Exists(originalFullPath))
                {
                    throw new FileNotFoundException($"El archivo original no existe: {originalFileId}");
                }

                // Generar nuevo ID manteniendo la extensión
                var extension = Path.GetExtension(originalFileId);
                var newFileId = $"{Guid.NewGuid()}{extension}";

                // Construir ruta del nuevo archivo
                var newRelativePath = string.IsNullOrEmpty(subfolder)
                    ? newFileId
                    : Path.Combine(subfolder, newFileId);

                var newFullPath = Path.Combine(_uploadsPath, newRelativePath);

                // Crear directorio si no existe
                var directory = Path.GetDirectoryName(newFullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Copiar archivo
                File.Copy(originalFullPath, newFullPath);

                _logger.LogInformation("Archivo copiado: {OriginalPath} -> {NewPath}", originalFullPath, newFullPath);

                // Generar URL accesible
                var fileUrl = GenerateFileUrl(newRelativePath);

                return (newFileId, fileUrl);
            }
            catch (FileNotFoundException)
            {
                throw; // Re-throw file not found
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al copiar archivo localmente");
                throw new Exception($"Error al copiar archivo: {e.Message}", e);
            }
        }

        /// <summary>
        /// Construye la ruta relativa del archivo
        /// </summary>
        private static string BuildRelativePath(string userId, string fileId, string? subfolder)
        {
            // Formato: {userId}/{subfolder}/{fileId}
            // o: {userId}/{fileId} si no hay subfolder

            if (!string.IsNullOrWhiteSpace(subfolder))
            {
                return Path.Combine(userId, subfolder, fileId);
            }

            return Path.Combine(userId, fileId);
        }

        /// <summary>
        /// Genera la URL pública para acceder al archivo
        /// </summary>
        private string GenerateFileUrl(string relativePath)
        {
            // Obtener la URL base del servidor
            var request = _httpContextAccessor.HttpContext?.Request;

            if (request != null)
            {
                var scheme = request.Scheme;
                var host = request.Host.Value;
                var pathBase = request.PathBase.Value;

                // Normalizar la ruta relativa para URLs (usar / en lugar de \)
                var urlPath = relativePath.Replace("\\", "/");

                return $"{scheme}://{host}{pathBase}/uploads/{urlPath}";
            }

            // Fallback si no hay HttpContext disponible
            _logger.LogWarning("HttpContext no disponible, usando URL relativa");
            return $"/uploads/{relativePath.Replace("\\", "/")}";
        }

        /// <summary>
        /// Genera una URL de descarga para archivos locales (no necesita prefirmado)
        /// </summary>
        public async Task<GenerateDownloadUrlResponseDTO> GeneratePresignedDownloadUrlAsync(
            string userId,
            GenerateDownloadUrlRequestDTO request)
        {
            try
            {
                // Construir la ruta relativa
                var relativePath = BuildRelativePath(userId, request.FileId, request.Subfolder);

                // Generar URL
                var fileUrl = GenerateFileUrl(relativePath);

                _logger.LogInformation("URL de descarga generada para archivo local: {FileId}", request.FileId);

                return await Task.FromResult(new GenerateDownloadUrlResponseDTO
                {
                    PresignedUrl = fileUrl,
                    ExpiresIn = 0 // Los archivos locales no expiran
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al generar URL de descarga local");
                throw new Exception($"Error al generar URL de descarga: {e.Message}", e);
            }
        }
    }
}
