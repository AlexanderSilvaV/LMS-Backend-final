using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class CursoDuplicacionDTO
    {
        [Required(ErrorMessage = "El NRC del curso original es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El NRC original debe ser un número positivo")]
        public int NrcOriginal { get; set; }
        
        [Required(ErrorMessage = "El nuevo NRC es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El nuevo NRC debe ser un número positivo")]
        public int NuevoNrc { get; set; }
        
        [Required(ErrorMessage = "El nombre del nuevo curso es requerido")]
        [StringLength(70, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 70 caracteres")]
    public string NuevoNombre { get; set; } = string.Empty;
        
        [StringLength(250, ErrorMessage = "La descripción no puede exceder 250 caracteres")]
        public string? NuevaDescripcion { get; set; }
        
        public bool Activo { get; set; } = true;
    }
}