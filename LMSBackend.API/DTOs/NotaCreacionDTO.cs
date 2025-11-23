using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class NotaCreacionDTO
    {
        [Required]
        [Range(1.0, 7.0, ErrorMessage = "La nota debe estar entre 1.0 y 7.0")]
        public decimal Calificacion { get; set; }

        [Required]
    public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public int EvaluacionId { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public int NumeroIntento { get; set; } = 1;

        public bool EsNotaFinal { get; set; } = true;
    }
}