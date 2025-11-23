using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class LaboratorioPreguntaDTO
    {
        public int IdPregunta { get; set; }
        public string Enunciado { get; set; } = string.Empty;
        public Dictionary<string, string> Opciones { get; set; } = new();
    }

    public class LaboratorioPreguntasResponseDTO
    {
        public int EvaluacionId { get; set; }
        public int CursoId { get; set; }
        public IReadOnlyCollection<LaboratorioPreguntaDTO> Preguntas { get; set; } = Array.Empty<LaboratorioPreguntaDTO>();
    }

    public class LaboratorioSeleccionResponseDTO
    {
        public string SesionId { get; set; } = string.Empty;
        public int EvaluacionId { get; set; }
        public int CursoId { get; set; }
        public string AlumnoId { get; set; } = string.Empty;
        public IReadOnlyCollection<LaboratorioPreguntaDTO> Preguntas { get; set; } = Array.Empty<LaboratorioPreguntaDTO>();
    }

    public class LaboratorioRespuestaSeleccionadaDTO
    {
        public int PreguntaId { get; set; }
        [Required]
        [RegularExpression("^[a-dA-D]$", ErrorMessage = "La opción debe estar entre a y d")]
        public string Seleccion { get; set; } = string.Empty;
    }

    public class LaboratorioResultadosRequestDTO
    {
        [Required]
        public string SesionId { get; set; } = string.Empty;
        [Required]
        public int EvaluacionId { get; set; }
        [Required]
        public string AlumnoId { get; set; } = string.Empty;
        [MinLength(1)]
        public List<LaboratorioRespuestaSeleccionadaDTO> Respuestas { get; set; } = new();
    }

    public class LaboratorioRespuestaDetalleDTO
    {
        public int PreguntaId { get; set; }
        public string Enunciado { get; set; } = string.Empty;
        public string Seleccion { get; set; } = string.Empty;
        public bool EsCorrecta { get; set; }
        public decimal PuntosOtorgados { get; set; }
        public decimal PuntosPregunta { get; set; }
        public string? Retroalimentacion { get; set; }
    }

    public class LaboratorioResultadosResponseDTO
    {
        public int EvaluacionId { get; set; }
        public string AlumnoId { get; set; } = string.Empty;
        public decimal PuntajeObtenido { get; set; }
        public decimal PuntajeMaximo { get; set; }
        public decimal Calificacion { get; set; }
        public IReadOnlyCollection<LaboratorioRespuestaDetalleDTO> Detalle { get; set; } = Array.Empty<LaboratorioRespuestaDetalleDTO>();
        public DateTime FechaCalificacion { get; set; } = DateTime.UtcNow;
    }
}
