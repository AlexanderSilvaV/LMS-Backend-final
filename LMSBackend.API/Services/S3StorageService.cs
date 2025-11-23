using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using LMSBackend.API.Configuration;
using LMSBackend.API.DTOs;
using LMSBackend.API.Validators;

namespace LMSBackend.API.Services
{
    /// <summary>
    /// Implementación SEGURA de IStorageService usando Amazon S3 con URLs prefirmadas y encriptación
    /// </summary>
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AWSOptions _options;
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IOptions<AWSOptions> options, ILogger<S3StorageService> logger)
        {
            _logger = logger;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.BucketName))
            {
                throw new ArgumentNullException(nameof(_options.BucketName), "AWS BucketName no configurado");
            }

            if (string.IsNullOrWhiteSpace(_options.AccessKey))
            {
                throw new ArgumentNullException(nameof(_options.AccessKey), "AWS AccessKey no configurada");
            }

            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new ArgumentNullException(nameof(_options.SecretKey), "AWS SecretKey no configurada");
            }

            // Configurar cliente S3 con credenciales
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
            var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region);

            _s3Client = new AmazonS3Client(awsCredentials, regionEndpoint);

            _logger.LogInformation("S3StorageService inicializado SEGURO - Bucket: {BucketName}, Region: {Region}",
                _options.BucketName, _options.Region);
        }

        /// <summary>
        /// Genera una URL prefirmada para SUBIR un archivo (PUT) con encriptación y sin acceso público
        /// </summary>
        public async Task<GenerateUploadUrlResponseDTO> GeneratePresignedUploadUrlAsync(
            string userId,
            GenerateUploadUrlRequestDTO request)
        {
            try
            {
                // 1. Validar archivo
                var (isValid, errorMessage) = FileValidator.ValidateFile(
                    request.FileName,
                    request.ContentType,
                    request.FileSize);

                if (!isValid)
                {
                    throw new ArgumentException(errorMessage);
                }

                // 2. Sanitizar nombre del archivo
                var sanitizedFileName = FileValidator.SanitizeFileName(request.FileName);
                var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();

                // 3. Generar ID único para el archivo
                var fileId = $"{Guid.NewGuid()}{extension}";

                // 4. Construir la key S3 (ruta dentro del bucket)
                var s3Key = BuildS3Key(userId, fileId, request.Subfolder);

                // 5. Crear request de URL prefirmada para PUT con ENCRIPTACIÓN
                var presignRequest = new GetPreSignedUrlRequest
                {
                    BucketName = _options.BucketName,
                    Key = s3Key,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes),
                    ContentType = request.ContentType,

                    // ✅ SEGURIDAD: Encriptación obligatoria en reposo (SSE-S3/AES256)
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // ✅ SEGURIDAD: NO se usa CannedACL.PublicRead - el archivo es privado
                // Los archivos solo son accesibles mediante URLs prefirmadas

                // 6. Generar URL prefirmada
                var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

                _logger.LogInformation("URL prefirmada de upload generada - Key: {S3Key}, Expira en: {Minutes} minutos",
                    s3Key, _options.PresignedUrlExpirationMinutes);

                return new GenerateUploadUrlResponseDTO
                {
                    FileId = fileId,
                    PresignedUrl = presignedUrl,
                    ExpiresIn = _options.PresignedUrlExpirationMinutes * 60, // en segundos
                    S3Key = s3Key
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error de S3 al generar URL prefirmada de upload");
                throw new Exception($"Error al generar URL prefirmada: {e.Message}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al generar URL prefirmada de upload");
                throw;
            }
        }

        /// <summary>
        /// Genera una URL prefirmada para DESCARGAR un archivo (GET)
        /// </summary>
        public async Task<GenerateDownloadUrlResponseDTO> GeneratePresignedDownloadUrlAsync(
            string userId,
            GenerateDownloadUrlRequestDTO request)
        {
            try
            {
                // Construir la key S3
                var s3Key = BuildS3Key(userId, request.FileId, request.Subfolder);

                // Nota: No verificamos existencia del archivo para URLs prefirmadas
                // Si el archivo no existe, S3 devolverá 404 cuando se acceda a la URL
                // Esto evita llamadas adicionales a S3 y mejora el rendimiento

                // Crear request de URL prefirmada para GET
                var presignRequest = new GetPreSignedUrlRequest
                {
                    BucketName = _options.BucketName,
                    Key = s3Key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes)
                };

                // Opcional: forzar descarga con nombre específico
                if (!string.IsNullOrWhiteSpace(request.DownloadFileName))
                {
                    var sanitizedName = FileValidator.SanitizeFileName(request.DownloadFileName);
                    presignRequest.ResponseHeaderOverrides.ContentDisposition =
                        $"attachment; filename=\"{sanitizedName}\"";
                }

                // Generar URL prefirmada
                var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

                _logger.LogInformation("URL prefirmada de download generada - Key: {S3Key}, Expira en: {Minutes} minutos",
                    s3Key, _options.PresignedUrlExpirationMinutes);

                return new GenerateDownloadUrlResponseDTO
                {
                    PresignedUrl = presignedUrl,
                    ExpiresIn = _options.PresignedUrlExpirationMinutes * 60 // en segundos
                };
            }
            catch (FileNotFoundException)
            {
                throw; // Re-throw file not found
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error de S3 al generar URL prefirmada de download");
                throw new Exception($"Error al generar URL prefirmada: {e.Message}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al generar URL prefirmada de download");
                throw;
            }
        }

        /// <summary>
        /// Guarda un archivo en S3 usando el método tradicional (para compatibilidad con código existente)
        /// IMPORTANTE: Este método ahora usa encriptación y sin ACL público
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

                // 4. Construir la key S3
                var s3Key = BuildS3Key(userId, fileId, subfolder);

                // 5. Preparar request de subida con ENCRIPTACIÓN
                using var stream = archivo.OpenReadStream();
                var putRequest = new PutObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = s3Key,
                    InputStream = stream,
                    ContentType = archivo.ContentType,

                    // ✅ SEGURIDAD: Encriptación obligatoria en reposo (SSE-S3/AES256)
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // ✅ SEGURIDAD: NO se usa CannedACL.PublicRead
                // El archivo es privado por defecto

                // 6. Subir archivo
                await _s3Client.PutObjectAsync(putRequest);

                _logger.LogInformation("Archivo subido exitosamente a S3 (PRIVADO + ENCRIPTADO): {S3Key}", s3Key);

                // 7. Generar URL prefirmada para descarga (válida por 15 minutos)
                var downloadUrl = await GeneratePresignedDownloadUrlAsync(userId, new GenerateDownloadUrlRequestDTO
                {
                    FileId = fileId,
                    Subfolder = subfolder
                });

                return (fileId, downloadUrl.PresignedUrl);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error de S3 al subir archivo");
                throw new Exception($"Error al subir archivo a S3: {e.Message}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al subir archivo");
                throw;
            }
        }

        /// <summary>
        /// Elimina un archivo de S3
        /// </summary>
        public async Task DeleteFileAsync(string fileId, string subfolder)
        {
            try
            {
                // Nota: userId no se pasa en la interfaz original, se usa una key genérica
                var s3Key = string.IsNullOrEmpty(subfolder)
                    ? fileId
                    : $"{subfolder}/{fileId}";

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = s3Key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);

                _logger.LogInformation("Archivo eliminado exitosamente de S3: {S3Key}", s3Key);
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error de S3 al eliminar archivo");
                throw new Exception($"Error al eliminar archivo de S3: {e.Message}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al eliminar archivo");
                throw;
            }
        }

        /// <summary>
        /// Copia un archivo existente en S3 creando una nueva copia con ID único
        /// IMPORTANTE: Este método ahora usa encriptación y sin ACL público
        /// </summary>
        public async Task<(string fileId, string fileUrl)> CopyFileAsync(string originalFileId, string subfolder)
        {
            try
            {
                // Construir keys S3
                var originalKey = string.IsNullOrEmpty(subfolder)
                    ? originalFileId
                    : $"{subfolder}/{originalFileId}";

                // Generar nuevo ID manteniendo la extensión
                var extension = Path.GetExtension(originalFileId);
                var newFileId = $"{Guid.NewGuid()}{extension}";
                var newKey = string.IsNullOrEmpty(subfolder)
                    ? newFileId
                    : $"{subfolder}/{newFileId}";

                // Copiar objeto en S3 con ENCRIPTACIÓN
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _options.BucketName,
                    SourceKey = originalKey,
                    DestinationBucket = _options.BucketName,
                    DestinationKey = newKey,

                    // ✅ SEGURIDAD: Encriptación obligatoria en reposo (SSE-S3/AES256)
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // ✅ SEGURIDAD: NO se usa CannedACL.PublicRead

                await _s3Client.CopyObjectAsync(copyRequest);

                _logger.LogInformation("Archivo copiado exitosamente en S3 (PRIVADO + ENCRIPTADO): {OriginalKey} -> {NewKey}",
                    originalKey, newKey);

                // Generar URL prefirmada para el nuevo archivo
                var downloadUrl = await GeneratePresignedDownloadUrlAsync("system", new GenerateDownloadUrlRequestDTO
                {
                    FileId = newFileId,
                    Subfolder = subfolder
                });

                return (newFileId, downloadUrl.PresignedUrl);
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error de S3 al copiar archivo");
                throw new Exception($"Error al copiar archivo en S3: {e.Message}", e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error al copiar archivo");
                throw;
            }
        }

        /// <summary>
        /// Verifica si un archivo existe en S3
        /// </summary>
        private async Task<bool> FileExistsAsync(string s3Key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _options.BucketName,
                    Key = s3Key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary>
        /// Construye la key S3 de forma consistente
        /// </summary>
        private static string BuildS3Key(string userId, string fileId, string? subfolder)
        {
            // Formato: uploads/{userId}/{subfolder}/{fileId}
            // o: uploads/{userId}/{fileId} si no hay subfolder

            var basePath = $"uploads/{userId}";

            if (!string.IsNullOrWhiteSpace(subfolder))
            {
                return $"{basePath}/{subfolder}/{fileId}";
            }

            return $"{basePath}/{fileId}";
        }
    }
}
