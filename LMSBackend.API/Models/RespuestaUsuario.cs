using System;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class RespuestaUsuario
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = null!;
        
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;
        
    [Required]
    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; } = null!;
        
    [Required]
    public int OpcionId { get; set; }
    public Opcion Opcion { get; set; } = null!;
        
        public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;
        public int NumeroIntento { get; set; } = 1;
    }
}