using System.Linq;

namespace LMSBackend.API.Validators
{
    /// <summary>
    /// Validador de archivos para garantizar seguridad en uploads
    /// </summary>
    public static class FileValidator
    {
        // Tamaño máximo: 50 MB
        public const long MaxFileSizeBytes = 50 * 1024 * 1024;

        // Extensiones prohibidas por razones de seguridad
        private static readonly string[] ProhibitedExtensions = new[]
        {
            ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".scr",
            ".vbs", ".js", ".jar", ".ps1", ".sh", ".app", ".deb",
            ".rpm", ".dmg", ".pkg", ".run"
        };

        // Extensiones permitidas
        private static readonly string[] AllowedExtensions = new[]
        {
            // Documentos
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".rtf", ".odt", ".ods", ".odp",

            // Imágenes
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp",
            ".ico", ".tiff", ".tif",

            // Audio
            ".mp3", ".wav", ".ogg", ".m4a", ".flac", ".aac",

            // Video
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv",

            // Comprimidos
            ".zip", ".rar", ".7z", ".tar", ".gz",

            // Código fuente (para tareas de programación)
            ".cs", ".java", ".py", ".cpp", ".c", ".h", ".php",
            ".html", ".css", ".xml", ".json", ".sql"
        };

        // MIME types permitidos
        private static readonly string[] AllowedMimeTypes = new[]
        {
            // Documentos
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "text/plain",
            "application/rtf",

            // Imágenes
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/svg+xml",
            "image/webp",
            "image/tiff",

            // Audio
            "audio/mpeg",
            "audio/wav",
            "audio/ogg",
            "audio/mp4",
            "audio/flac",
            "audio/aac",

            // Video
            "video/mp4",
            "video/x-msvideo",
            "video/quicktime",
            "video/x-ms-wmv",
            "video/x-flv",
            "video/webm",
            "video/x-matroska",

            // Comprimidos
            "application/zip",
            "application/x-rar-compressed",
            "application/x-7z-compressed",
            "application/x-tar",
            "application/gzip",

            // Código fuente
            "text/x-csharp",
            "text/x-java-source",
            "text/x-python",
            "text/x-c",
            "text/html",
            "text/css",
            "application/xml",
            "application/json",
            "application/sql",

            // Genéricos para archivos de texto
            "text/xml",
            "application/octet-stream" // Solo para archivos zip/comprimidos conocidos
        };

        /// <summary>
        /// Valida un archivo para garantizar seguridad
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateFile(string fileName, string contentType, long fileSize)
        {
            // 1. Validar que el archivo tiene nombre
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return (false, "El nombre del archivo es requerido");
            }

            // 2. Validar extensión
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                return (false, "El archivo debe tener una extensión válida");
            }

            // 3. Verificar extensiones prohibidas
            if (ProhibitedExtensions.Contains(extension))
            {
                return (false, $"La extensión {extension} está prohibida por razones de seguridad");
            }

            // 4. Verificar extensiones permitidas
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, $"La extensión {extension} no está permitida. Tipos permitidos: PDF, Office, imágenes, audio, video, comprimidos, código fuente");
            }

            // 5. Validar MIME type
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return (false, "El Content-Type es requerido");
            }

            // Normalizar MIME type (remover parámetros como charset)
            var mimeType = contentType.Split(';')[0].Trim().ToLowerInvariant();

            if (!AllowedMimeTypes.Contains(mimeType))
            {
                // Permitir application/octet-stream solo para extensiones de archivo comprimido
                if (mimeType == "application/octet-stream")
                {
                    var compressedExtensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
                    if (!compressedExtensions.Contains(extension))
                    {
                        return (false, $"El tipo MIME {contentType} no está permitido para archivos {extension}");
                    }
                }
                else
                {
                    return (false, $"El tipo MIME {contentType} no está permitido");
                }
            }

            // 6. Validar tamaño máximo (50 MB)
            if (fileSize > MaxFileSizeBytes)
            {
                var maxSizeMB = MaxFileSizeBytes / (1024 * 1024);
                return (false, $"El archivo excede el tamaño máximo permitido de {maxSizeMB} MB");
            }

            // 7. Validar tamaño mínimo (1 byte)
            if (fileSize <= 0)
            {
                return (false, "El archivo está vacío");
            }

            // Todas las validaciones pasaron
            return (true, string.Empty);
        }

        /// <summary>
        /// Sanitiza el nombre del archivo para evitar ataques de path traversal
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "unnamed";
            }

            // Remover caracteres peligrosos
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Remover intentos de path traversal
            sanitized = sanitized.Replace("..", "").Replace("/", "").Replace("\\", "");

            // Limitar longitud
            if (sanitized.Length > 200)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExt.Substring(0, 200 - extension.Length) + extension;
            }

            return sanitized;
        }
    }
}
