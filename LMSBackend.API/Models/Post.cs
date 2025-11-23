using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class Post
    {
        public int PostId { get; set; }

        [Required]
        [MaxLength(10000)]
        public string? Contenido { get; set; }
        public bool Editado { get; set; }

        public string? AutorId { get; set; }

        public Usuario? Autor { get; set; }

        public DateTime FechaCreacion { get; set; }

        public int? ParentPostId { get; set; }

        public int HiloId { get; set; }

        public DateTime? EditedAt { get; set; }

        public bool SoftDeleted { get; set; }

        public Hilo? Hilo { get; set; }
    }
}
