namespace LMSBackend.API.Configuration
{
    /// <summary>
    /// Opciones de configuración para AWS S3
    /// </summary>
    public class AWSOptions
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-2";
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo de expiración para URLs prefirmadas en minutos (default: 15 minutos)
        /// </summary>
        public int PresignedUrlExpirationMinutes { get; set; } = 15;

        /// <summary>
        /// Tamaño máximo de archivo en bytes (default: 50 MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
    }
}
