using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class NotasEstudianteDTO
    {
        public int EvaluacionId { get; set; }
        public string EvaluacionTitulo { get; set; } = string.Empty;
        public string CursoNombre { get; set; } = string.Empty;
        public string DocenteNombre { get; set; } = string.Empty;
        public List<NotaDTO> Notas { get; set; } = new();
        public decimal? MejorNota { get; set; }
        public decimal? NotaFinal { get; set; }
        public bool EsLaboratorio3DLab { get; set; }
    }
}