
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class ForoEdicionDTO
    {
        // Si viene null, no se edita. Si viene string vacío, decide en service si normalizas a null.
        [StringLength(120, ErrorMessage = "El título no puede superar 120 caracteres.")]
        public string? Titulo { get; set; }

        [StringLength(2000, ErrorMessage = "La descripción no puede superar 2000 caracteres.")]
        public string? Descripcion { get; set; }
    }
}
