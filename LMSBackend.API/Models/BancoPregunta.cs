using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSBackend.API.Models
{
    public class BancoPregunta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(1000)]
    public string Texto { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Categoria { get; set; }

        [Required]
    public string DocenteId { get; set; } = string.Empty;
    public Usuario Docente { get; set; } = null!;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }

        [Range(1, 100)]
        public int Puntos { get; set; } = 1;

        public bool Activa { get; set; } = true;


        // Columnas adicionales requeridas por la estructura existente de la DB
        [Required]
        public int Dificultad { get; set; } = 1;

        [Required]
        public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

        [Required]
    public string TextoNormalizado { get; set; } = string.Empty;

        public string? Tema { get; set; }
        public int? CursoNrc { get; set; }
        public int? ModuloId { get; set; }
        public string? AutorId { get; set; }

        [MaxLength(2000)]
        public string? Retroalimentacion { get; set; }

        public ICollection<OpcionBanco> Opciones { get; set; } = new List<OpcionBanco>();
    }
}