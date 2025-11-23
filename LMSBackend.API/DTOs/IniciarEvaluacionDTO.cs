using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class IniciarEvaluacionDTO
    {
        [Required]
        public int EvaluacionId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public int TiempoLimiteMins { get; set; }

        [Required]
        public int NumeroIntento { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
