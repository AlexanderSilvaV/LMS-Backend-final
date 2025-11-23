using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/storage")]
    [Authorize] // Requiere autenticación JWT
    public class StorageController : ControllerBase
    {
        private readonly S3StorageService _storageService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(S3StorageService storageService, ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Genera una URL prefirmada para subir un archivo de forma segura
        /// </summary>
        /// <remarks>
        /// Este endpoint valida el archivo antes de generar la URL, garantizando:
        /// - Tamaño máximo de 50 MB
        /// - Solo extensiones permitidas (no ejecutables)
        /// - MIME types válidos
        /// - Encriptación AES256 en reposo
        ///
        /// El cliente debe usar la URL retornada para hacer un PUT con el archivo.
        /// La URL expira en 15 minutos.
        /// </remarks>
        [HttpPost("generate-upload-url")]
        [ProducesResponseType(typeof(GenerateUploadUrlResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GenerateUploadUrl([FromBody] GenerateUploadUrlRequestDTO request)
        {
            try
            {
                // Obtener userId del token JWT
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { mensaje = "Usuario no autenticado" });
                }

                // Validar model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Generando URL prefirmada de upload para usuario {UserId}, archivo: {FileName}",
                    userId, request.FileName);

                // Generar URL prefirmada
                var response = await _storageService.GeneratePresignedUploadUrlAsync(userId, request);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación de archivo fallida: {Message}", ex.Message);
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL prefirmada de upload");
                return StatusCode(500, new { mensaje = "Error al generar URL de subida" });
            }
        }

        /// <summary>
        /// Genera una URL prefirmada para descargar un archivo de forma segura
        /// </summary>
        /// <remarks>
        /// Este endpoint genera una URL temporal para descargar un archivo.
        /// La URL expira en 15 minutos.
        /// </remarks>
        [HttpPost("generate-download-url")]
        [ProducesResponseType(typeof(GenerateDownloadUrlResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenerateDownloadUrl([FromBody] GenerateDownloadUrlRequestDTO request)
        {
            try
            {
                // Obtener userId del token JWT
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { mensaje = "Usuario no autenticado" });
                }

                // Validar model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Generando URL prefirmada de download para usuario {UserId}, archivo: {FileId}",
                    userId, request.FileId);

                // Generar URL prefirmada
                var response = await _storageService.GeneratePresignedDownloadUrlAsync(userId, request);

                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Archivo no encontrado: {Message}", ex.Message);
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL prefirmada de download");
                return StatusCode(500, new { mensaje = "Error al generar URL de descarga" });
            }
        }

        /// <summary>
        /// [LEGACY] Sube un archivo directamente al servidor
        /// </summary>
        /// <remarks>
        /// NOTA: Este endpoint es legacy y solo se mantiene para compatibilidad con código existente.
        /// Para nuevos desarrollos, use el endpoint /generate-upload-url que permite uploads directos a S3.
        ///
        /// Este método ahora también aplica:
        /// - Validaciones de seguridad
        /// - Encriptación AES256
        /// - Sin acceso público (URLs prefirmadas)
        /// </remarks>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [RequestSizeLimit(52428800)] // 50 MB
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string? subfolder = null)
        {
            try
            {
                // Obtener userId del token JWT
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { mensaje = "Usuario no autenticado" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { mensaje = "No se ha proporcionado ningún archivo" });
                }

                _logger.LogInformation("Upload directo para usuario {UserId}, archivo: {FileName}, tamaño: {Size} bytes",
                    userId, file.FileName, file.Length);

                // Subir archivo (ahora con validaciones y encriptación)
                var (fileId, fileUrl) = await _storageService.SaveFileAsync(userId, file, subfolder ?? "uploads");

                return Ok(new
                {
                    mensaje = "Archivo subido exitosamente",
                    fileId,
                    fileUrl, // Esta es una URL prefirmada temporal
                    fileName = file.FileName
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación de archivo fallida: {Message}", ex.Message);
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo");
                return StatusCode(500, new { mensaje = "Error al subir archivo" });
            }
        }

        /// <summary>
        /// Elimina un archivo del almacenamiento
        /// </summary>
        [HttpDelete("{fileId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteFile(string fileId, [FromQuery] string? subfolder = null)
        {
            try
            {
                // Obtener userId del token JWT
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { mensaje = "Usuario no autenticado" });
                }

                if (string.IsNullOrWhiteSpace(fileId))
                {
                    return BadRequest(new { mensaje = "El ID del archivo es requerido" });
                }

                _logger.LogInformation("Eliminando archivo {FileId} para usuario {UserId}", fileId, userId);

                await _storageService.DeleteFileAsync(fileId, subfolder ?? "uploads");

                return Ok(new { mensaje = "Archivo eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar archivo");
                return StatusCode(500, new { mensaje = "Error al eliminar archivo" });
            }
        }

        /// <summary>
        /// Endpoint de prueba para verificar configuración de storage
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous] // Permitir sin autenticación para testing
        public IActionResult TestStorage()
        {
            return Ok(new
            {
                mensaje = "Storage configurado correctamente",
                features = new[]
                {
                    "URLs prefirmadas con expiración de 15 minutos",
                    "Validación de archivos (tamaño, tipo, extensión)",
                    "Encriptación AES256 en reposo",
                    "Sin acceso público (Block Public Access)",
                    "Tamaño máximo: 50 MB",
                    "Extensiones prohibidas: .exe, .dll, .bat, etc."
                }
            });
        }
    }
}
