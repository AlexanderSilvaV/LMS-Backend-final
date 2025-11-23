using System;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

        [Required]
    public string DocenteId { get; set; } = string.Empty;
    public Usuario Docente { get; set; } = null!;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    }
}