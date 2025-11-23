
namespace LMSBackend.API.DTOs
{
    public class CursoBusquedaDTO
    {
        public int? Nrc { get; set; }
        public string? Nombre { get; set; }
        public bool? Activo { get; set; }
        public int Pagina { get; set; }
        public int CantidadPorPagina { get; set; }
    }
}
