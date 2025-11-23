using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Asunto { get; set; } = string.Empty;

        [Required]
        public string Destinatario { get; set; } = string.Empty;

        [Required]
        public string Estado { get; set; } = string.Empty;

        public int? EvaluacionId { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaEnvio { get; set; }

        public int? HiloId { get; set; }

        [Required]
        public int IntentosEnvio { get; set; }

        public string? MensajeError { get; set; }

        public int? PostId { get; set; }

        public string? SendGridMessageId { get; set; }

        [Required]
        public string TipoNotificacion { get; set; } = string.Empty;
    }
}
