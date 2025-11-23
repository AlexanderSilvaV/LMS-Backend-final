using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class PreguntaDTO
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int Orden { get; set; }
        public int Puntos { get; set; }
        public int EvaluacionId { get; set; }
        public List<OpcionDTO> Opciones { get; set; } = new();
    }
}