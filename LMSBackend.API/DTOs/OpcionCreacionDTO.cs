using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class OpcionCreacionDTO
    {
        [Required]
        [MaxLength(300)]
    public string Texto { get; set; } = string.Empty;
        
        public bool EsCorrecta { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor a 0")]
        public int Orden { get; set; }
    }
}