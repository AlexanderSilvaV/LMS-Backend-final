using System;
using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class EvaluacionDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = string.Empty;
        public string DocenteId { get; set; } = string.Empty;
        public string DocenteNombre { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int TiempoLimiteMins { get; set; }
        public bool Activa { get; set; }
        public int IntentosMaximos { get; set; }
        public int TotalPreguntas { get; set; }
        public List<PreguntaDTO> Preguntas { get; set; } = new();

        // Propiedades para Laboratorio 3DLab
        public bool EsLaboratorio3DLab { get; set; }
        public int PreguntasMinimasLaboratorio { get; set; }
        public int PreguntasPorSesionLaboratorio { get; set; }
    }
}