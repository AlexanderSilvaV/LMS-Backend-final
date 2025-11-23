using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Pregunta
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
    public string Texto { get; set; } = string.Empty;
        
        public int Orden { get; set; }
        public int Puntos { get; set; } = 1;
        
        [Required]
        public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;
        
        public ICollection<Opcion> Opciones { get; set; } = new List<Opcion>();
    }
}