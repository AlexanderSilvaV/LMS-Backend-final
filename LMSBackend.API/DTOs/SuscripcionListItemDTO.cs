
namespace LMSBackend.API.DTOs
{
    public class SuscripcionListItemDTO
    {
        public int HiloId { get; set; }
        public int ForoId { get; set; }

        public string HiloTitulo { get; set; } = string.Empty;
        public string ForoTitulo { get; set; } = string.Empty;

        public bool Cerrado { get; set; }
        public bool Pinned { get; set; }
        public int? PinnedOrder { get; set; }

        public DateTime LastActivityAt { get; set; }
        public DateTime FechaSuscripcion { get; set; }
    }
}
