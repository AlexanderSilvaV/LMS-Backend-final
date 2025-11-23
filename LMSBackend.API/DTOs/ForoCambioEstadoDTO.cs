using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    /// <summary>
    /// Body para cambiar el estado de un foro.
    /// Estados permitidos: "Activo" | "Cerrado" | "Archivado".
    /// </summary>
    public class ForoCambioEstadoDTO
    {
        [Required(ErrorMessage = "Debes indicar el nuevo estado.")]
        [RegularExpression("^(Activo|Cerrado|Archivado)$", ErrorMessage = "Estado inválido. Usa: Activo, Cerrado o Archivado.")]
        public string NuevoEstado { get; set; } = string.Empty;
    }
}
