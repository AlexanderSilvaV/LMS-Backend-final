using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class PreguntaCreacionDTO
    {
        [Required]
        [MaxLength(500)]
    public string Texto { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor a 0")]
        public int Orden { get; set; }
        
        [Range(1, 100, ErrorMessage = "Los puntos deben estar entre 1 y 100")]
        public int Puntos { get; set; } = 1;
        
        [Required(ErrorMessage = "Debe incluir opciones de respuesta")]
        [MinLength(2, ErrorMessage = "Debe incluir al menos 2 opciones de respuesta")]
        public List<OpcionCreacionDTO> Opciones { get; set; } = new();
    }
}