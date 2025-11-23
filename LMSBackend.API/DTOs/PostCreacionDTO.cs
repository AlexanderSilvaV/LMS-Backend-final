
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class PostCreacionDTO
    {
        [Required]
        public int HiloId { get; set; }

        [Required]
        [StringLength(10000, ErrorMessage = "El contenido no puede superar 10000 caracteres.")]
        public string Contenido { get; set; } = string.Empty;
    }
}
