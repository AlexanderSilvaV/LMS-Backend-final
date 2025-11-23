using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Foro
    {

        public int ForoId { get; set; }

        [Required]
        [MaxLength(120)]
        public required string Titulo { get; set; }

        [MaxLength(2000)]
        public string? Descripcion { get; set; }

        public Estado Estado { get; set; }

        public DateTime FechaCreacion { get; set; }

        public int ModuloId { get; set; }

        public required Modulo Modulo { get; set; }

        public bool AllowStudentThreads { get; set; }

        public bool RequireInitialPostToView { get; set; }

        public required string CreadorId { get; set; }
        public Usuario? Creador { get; set; }

        public ICollection<Hilo> Hilos { get; set; } = new List<Hilo>();
    }

    public enum Estado
    {
        Activo,
        Cerrado,
        Archivado
    }
}
