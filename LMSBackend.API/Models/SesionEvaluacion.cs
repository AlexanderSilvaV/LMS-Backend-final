using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace LMSBackend.API.Models
{
    public class SesionEvaluacion
    {
        [Key]
        public int Id { get; set; }
        
    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;
        
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;
        
        public int NumeroIntento { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime? FechaFinalizacion { get; set; }
    public string Token { get; set; } = string.Empty;
        public bool Activa { get; set; } = true;
        public bool Completada { get; set; } = false;
        public bool TiempoAgotado { get; set; } = false;

        // Metadata para almacenar información adicional (ej: preguntas seleccionadas en laboratorio 3DLAB)
        public string? Metadata { get; set; }

        public DateTime FechaLimite => FechaInicio.AddMinutes(Evaluacion?.TiempoLimiteMins ?? 60);
        public bool EstaVencida => DateTime.UtcNow > FechaLimite;

        public SesionEvaluacion()
        {
            Token = GenerarTokenSeguro();
        }

        private static string GenerarTokenSeguro()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }
    }
}