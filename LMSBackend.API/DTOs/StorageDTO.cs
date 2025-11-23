using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    /// <summary>
    /// Request para generar una URL prefirmada de subida
    /// </summary>
    public class GenerateUploadUrlRequestDTO
    {
        [Required(ErrorMessage = "El nombre del archivo es requerido")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de contenido es requerido")]
        public string ContentType { get; set; } = string.Empty;

        [Range(1, long.MaxValue, ErrorMessage = "El tamaño del archivo debe ser mayor a 0")]
        public long FileSize { get; set; }

        /// <summary>
        /// Subcarpeta opcional donde se guardará el archivo
        /// </summary>
        public string? Subfolder { get; set; }
    }

    /// <summary>
    /// Response con la URL prefirmada para subir el archivo
    /// </summary>
    public class GenerateUploadUrlResponseDTO
    {
        /// <summary>
        /// ID único del archivo (para referencias futuras)
        /// </summary>
        public string FileId { get; set; } = string.Empty;

        /// <summary>
        /// URL prefirmada para subir el archivo (método PUT)
        /// </summary>
        public string PresignedUrl { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo de expiración en segundos
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Key del archivo en S3 (para referencia)
        /// </summary>
        public string S3Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para generar una URL prefirmada de descarga
    /// </summary>
    public class GenerateDownloadUrlRequestDTO
    {
        [Required(ErrorMessage = "El ID del archivo es requerido")]
        public string FileId { get; set; } = string.Empty;

        /// <summary>
        /// Subcarpeta donde está el archivo
        /// </summary>
        public string? Subfolder { get; set; }

        /// <summary>
        /// Nombre del archivo para la descarga (opcional)
        /// </summary>
        public string? DownloadFileName { get; set; }
    }

    /// <summary>
    /// Response con la URL prefirmada para descargar el archivo
    /// </summary>
    public class GenerateDownloadUrlResponseDTO
    {
        /// <summary>
        /// URL prefirmada para descargar el archivo (método GET)
        /// </summary>
        public string PresignedUrl { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo de expiración en segundos
        /// </summary>
        public int ExpiresIn { get; set; }
    }
}
