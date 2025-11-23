using System;

namespace LMSBackend.API.DTOs
{
    public class PostDTO
    {
        public int PostId { get; set; }
        public int HiloId { get; set; }
        public string AutorId { get; set; } = string.Empty;
        public string? AutorNombre { get; set; }
        public string Contenido { get; set; } = string.Empty;

        public bool Editado { get; set; }
        public DateTime? EditedAt { get; set; }

        public bool SoftDeleted { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
