using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class MaterialEdicionDTO
    {
        [MaxLength(70)]
        public string? Nombre { get; set; }

        [MaxLength(500)]
        public string? Ruta { get; set; }
    }
}
