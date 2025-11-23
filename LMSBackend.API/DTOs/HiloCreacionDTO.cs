
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class HiloCreacionDTO
    {
        [Required]
        public int ForoId { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = string.Empty;
        // El autorId lo toma el controller desde el token y se lo pasa al service.
    }
}
