using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LMSBackend.API.Validation;

namespace LMSBackend.API.DTOs
{
    public class EvaluacionCreacionDTO
    {
        [Required]
        [MaxLength(100)]
    public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public int CursoId { get; set; }

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        [RangoCondicional(5, 300, 0, nameof(EsLaboratorio3DLab), ErrorMessage = "El tiempo límite debe estar entre 5 y 300 minutos (o 0 para laboratorios 3DLAB)")]
        public int TiempoLimiteMins { get; set; } = 60;

        [RangoCondicional(1, 10, 0, nameof(EsLaboratorio3DLab), ErrorMessage = "Los intentos máximos deben estar entre 1 y 10 (o 0 para laboratorios 3DLAB)")]
        public int IntentosMaximos { get; set; } = 1;

        // Propiedades para Laboratorio 3DLab
        public bool EsLaboratorio3DLab { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Las preguntas mínimas deben estar entre 0 y 100")]
        public int PreguntasMinimasLaboratorio { get; set; } = 0;

        [Range(0, 100, ErrorMessage = "Las preguntas por sesión deben estar entre 0 y 100")]
        public int PreguntasPorSesionLaboratorio { get; set; } = 0;

        // Preguntas manuales
        public List<PreguntaCreacionDTO>? Preguntas { get; set; }
    }
}