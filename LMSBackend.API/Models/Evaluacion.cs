using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Evaluacion
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
    public string Titulo { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Descripcion { get; set; }
        
        [Required]
        public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;
        
        [Required]
    public string DocenteId { get; set; } = string.Empty;
    public Usuario Docente { get; set; } = null!;
        
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        
        public int TiempoLimiteMins { get; set; } = 60;
        public bool Activa { get; set; } = true;
        public int IntentosMaximos { get; set; } = 1;

        // Propiedades para Laboratorio 3DLab
        public bool EsLaboratorio3DLab { get; set; } = false;
        public int PreguntasMinimasLaboratorio { get; set; } = 0;
        public int PreguntasPorSesionLaboratorio { get; set; } = 0;

        // Preguntas manuales
        public ICollection<Pregunta> Preguntas { get; set; } = new List<Pregunta>();
        public ICollection<RespuestaUsuario> RespuestasUsuario { get; set; } = new List<RespuestaUsuario>();
    }
}