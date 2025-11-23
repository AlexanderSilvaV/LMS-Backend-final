using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class ForoListadoDTO
    {
        [Required]
        public int ModuloId { get; set; }

        // Acepta "Activo" | "Cerrado" | "Archivado" (opcional)
        [RegularExpression("^(Activo|Cerrado|Archivado)$", ErrorMessage = "Estado inválido.")]
        public string? Estado { get; set; }

        public bool IncluirArchivados { get; set; } = false;

        [Range(1, int.MaxValue, ErrorMessage = "La página debe ser >= 1.")]
        public int Pagina { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "La cantidad por página debe estar entre 1 y 50.")]
        public int CantidadPorPagina { get; set; } = 20;

        // Búsqueda por título (opcional)
        [StringLength(120, ErrorMessage = "El término de búsqueda no puede superar 120 caracteres.")]
        public string? Q { get; set; }
    }
}
