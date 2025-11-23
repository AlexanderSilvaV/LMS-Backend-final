using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Hilo
    {
        public int HiloId { get; set; }

        [Required]
        [MaxLength(120)]
        public required string Titulo { get; set; }

        public bool Cerrado { get; set; }

        public DateTime? UnlockAt { get; set; }
        public DateTime? LockAt { get; set; }
        public required string AutorId { get; set; }
        public Usuario? Autor { get; set; }
        public bool Pinned { get; set; }
        public int? PinnedOrder { get; set; }
        public DateTime LastActivityAt { get; set; }

        public DateTime FechaCreacion { get; set; }

        public int ForoId { get; set; }
        public required Foro Foro { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
