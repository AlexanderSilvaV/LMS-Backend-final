using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Opcion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(300)]
        public string Texto { get; set; } = string.Empty;
        
        public bool EsCorrecta { get; set; }
        public int Orden { get; set; }
        
        [Required]
        public int PreguntaId { get; set; }
        public Pregunta Pregunta { get; set; } = null!;
    }
}