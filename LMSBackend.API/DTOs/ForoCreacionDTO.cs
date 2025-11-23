
using System.ComponentModel.DataAnnotations;


namespace LMSBackend.API.DTOs
{
    public class ForoCreacionDTO
    {
        [Required]
        public int ModuloId { get; set; }

        [Required]
        [StringLength(120, ErrorMessage = "El título no puede superar 120 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "La descripción no puede superar 2000 caracteres.")]
        public string? Descripcion { get; set; }
        // Nota: El CreadorId se obtiene del token en el Controller/Service (recomendado).
    }
}
