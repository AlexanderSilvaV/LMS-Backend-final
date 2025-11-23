using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class BancoPreguntaEdicionDTO
    {
        [Required]
        [MaxLength(1000)]
    public string Enunciado { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Categoria { get; set; }

        [Range(1, 100, ErrorMessage = "Los puntos deben estar entre 1 y 100")]
        public int Puntos { get; set; } = 1;

        [Required(ErrorMessage = "Debe incluir opciones de respuesta")]
        [MinLength(2, ErrorMessage = "Debe incluir al menos 2 opciones de respuesta")]
        public List<OpcionBancoCreacionDTO> Opciones { get; set; } = new();


        public bool Activa { get; set; } = true;
    }
}