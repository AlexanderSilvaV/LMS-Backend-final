
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class HiloListadoDTO
    {
        public bool? Pinned { get; set; }
        public bool? Cerrado { get; set; }

        [Range(1, 50)]
        public int CantidadPorPagina { get; set; } = 20;

        [Range(1, int.MaxValue)]
        public int Pagina { get; set; } = 1;

        [StringLength(120)]
        public string? Q { get; set; }
    }
}
