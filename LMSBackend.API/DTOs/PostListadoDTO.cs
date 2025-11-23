
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class PostListadoDTO
    {
        [Range(1, int.MaxValue)]
        public int Pagina { get; set; } = 1;

        [Range(1, 100)]
        public int CantidadPorPagina { get; set; } = 20;
    }
}
