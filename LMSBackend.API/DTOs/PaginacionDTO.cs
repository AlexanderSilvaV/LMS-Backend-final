
namespace LMSBackend.API.DTOs
{
    public class PaginacionDTO
    {
        public int PaginaActual { get; set; }
        public int CantidadPorPagina { get; set; }
        public int TotalResultados { get; set; }
        public int TotalPaginas { get; set; }
    }
}
