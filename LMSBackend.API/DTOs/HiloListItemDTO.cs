
namespace LMSBackend.API.DTOs
{
    public class HiloListItemDTO
    {
        public int HiloId { get; set; }
        public int ForoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string AutorId { get; set; } = string.Empty;
        public string? AutorNombre { get; set; }

        public bool Cerrado { get; set; }
        public bool Pinned { get; set; }
        public int? PinnedOrder { get; set; }

        public DateTime LastActivityAt { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
