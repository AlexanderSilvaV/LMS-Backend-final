using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSBackend.API.Models
{
    public class Nota
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1.0, 7.0, ErrorMessage = "La nota debe estar entre 1.0 y 7.0")]
        [Column(TypeName = "decimal(3,1)")]
        public decimal Calificacion { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;

    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;

        public DateTime FechaCalificacion { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public int NumeroIntento { get; set; } = 1;

        public bool EsNotaFinal { get; set; } = true;

        public bool EsLaboratorio { get; set; } = false;
    }
}