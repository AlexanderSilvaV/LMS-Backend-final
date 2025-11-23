using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class Modulo
    {
        [Key]
        public int ModuloId { get; set; }
        [Required]
        [MaxLength(30)]
    public string Nombre { get; set; } = string.Empty;
        [Range(0, 100)]
        public int Indice { get; set; }
        public bool EsPredeterminado { get; set; }
        public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

        public List<Material> Materiales { get; set; } = new();

        public ICollection<Foro> Foros { get; set; } = new List<Foro>();
    }
}
