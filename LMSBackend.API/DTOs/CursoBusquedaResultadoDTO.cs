
using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class CursoBusquedaResultadoDTO
    {
        public List<CursoDTO> Cursos { get; set; } = new();
        public PaginacionDTO Paginacion { get; set; } = new();
    }
}
