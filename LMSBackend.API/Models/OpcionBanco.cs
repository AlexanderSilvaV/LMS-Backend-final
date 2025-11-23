using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class OpcionBanco
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
    public string Texto { get; set; } = string.Empty;

        public bool EsCorrecta { get; set; }

        public int Orden { get; set; }

        [Required]
        public int BancoPreguntaId { get; set; }
        public BancoPregunta BancoPregunta { get; set; } = null!;
    }
}