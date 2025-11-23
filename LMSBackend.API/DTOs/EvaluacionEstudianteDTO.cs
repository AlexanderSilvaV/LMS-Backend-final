namespace LMSBackend.API.DTOs
{
    public class EvaluacionEstudianteDTO
    {
        public int Id { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public int CursoId { get; set; }
        public string? CursoNombre { get; set; }
        public string? DocenteNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int TiempoLimiteMins { get; set; }
        public bool Activa { get; set; }
        public int IntentosMaximos { get; set; }
        public int TotalPreguntas { get; set; }
        public int IntentosRealizados { get; set; }
        public bool PuedeRealizarEvaluacion { get; set; }
        public bool YaCompletada { get; set; }
        public DateTime? UltimaFechaRealizada { get; set; }
        public double? MejorPorcentaje { get; set; }
        public bool EstaEnTiempo { get; set; }
        public CursoBasicoDTO? Curso { get; set; }
        public UltimoIntentoDTO? UltimoIntento { get; set; }
    }

    public class UltimoIntentoDTO
    {
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public double? Calificacion { get; set; }
        public string? Estado { get; set; } // "En Progreso", "Completado", "Tiempo Agotado"
    }

    public class CursoBasicoDTO
    {
        public string? Nombre { get; set; }
        public string? Nrc { get; set; }
    }

    public class EvaluacionParaRealizarDTO
    {
        public int Id { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public int TiempoLimiteMins { get; set; }
        public int NumeroIntento { get; set; }
        public DateTime FechaInicio { get; set; }
        public List<PreguntaEstudianteDTO> Preguntas { get; set; } = new();
    }

    public class PreguntaEstudianteDTO
    {
        public int Id { get; set; }
        public string? Texto { get; set; }
        public int Orden { get; set; }
        public int Puntos { get; set; }
        public List<OpcionEstudianteDTO> Opciones { get; set; } = new();
    }

    public class OpcionEstudianteDTO
    {
        public int Id { get; set; }
        public string? Texto { get; set; }
        public int Orden { get; set; }
    }

    public class EstadoEvaluacionDTO
    {
        public int EvaluacionId { get; set; }
        public int NumeroIntento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaLimite { get; set; }
        public int TiempoRestanteMins { get; set; }
        public bool TiempoAgotado { get; set; }
        public int PreguntasRespondidas { get; set; }
        public int TotalPreguntas { get; set; }
    }

    public class HistorialEvaluacionDTO
    {
        public int EvaluacionId { get; set; }
        public string? Titulo { get; set; }
        public string? CursoNombre { get; set; }
        public int NumeroIntento { get; set; }
        public DateTime FechaRealizada { get; set; }
        public int PuntajeObtenido { get; set; }
        public int PuntajeMaximo { get; set; }
        public double Porcentaje { get; set; }
        public bool Completada { get; set; }
        public string? Estado { get; set; } // "Completada", "En Progreso", "Tiempo Agotado"
    }
}