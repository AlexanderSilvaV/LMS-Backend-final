using System;
namespace LMSBackend.API.DTOs
{
    public class CursoDTO
    {
        public int Nrc { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? PortadaUrl { get; set; }
        public DateTime? PortadaActualizada { get; set; }
    }
}
