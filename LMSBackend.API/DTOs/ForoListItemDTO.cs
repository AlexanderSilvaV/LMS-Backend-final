using System;

namespace LMSBackend.API.DTOs
{
    public class ForoListItemDTO
    {
        public int ForoId { get; set; }
        public int ModuloId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}
