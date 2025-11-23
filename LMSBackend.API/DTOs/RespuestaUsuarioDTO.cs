using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class RespuestaUsuarioDTO
    {
        [Required]
        public int EvaluacionId { get; set; }
        
        [Required]
        public int PreguntaId { get; set; }
        
        [Required]
        public int OpcionId { get; set; }
    }
    
    public class SubmitRespuestasDTO
    {
        [Required]
        public int EvaluacionId { get; set; }
        
        [Required]
        public List<RespuestaUsuarioDTO> Respuestas { get; set; } = new();
    }
    
    public class ResultadoEvaluacionDTO
    {
        [Required]
        public int EvaluacionId { get; set; }
        
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        
        [Required]
        public string UsuarioNombre { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue)]
        public int PuntajeObtenido { get; set; }
        
        [Range(1, int.MaxValue)]
        public int PuntajeMaximo { get; set; }
        
        [Range(0, 100)]
        public double Porcentaje { get; set; }
        
        [Required]
        public DateTime FechaCompletado { get; set; }
        
        [Range(1, int.MaxValue)]
        public int NumeroIntento { get; set; }
        
        public List<DetalleRespuestaDTO> DetalleRespuestas { get; set; } = new();
    }
    
    public class DetalleRespuestaDTO
    {
        [Required]
        public int PreguntaId { get; set; }
        
        [Required]
        public string PreguntaTexto { get; set; } = string.Empty;
        
        [Required]
        public int OpcionSeleccionadaId { get; set; }
        
        [Required]
        public string OpcionSeleccionadaTexto { get; set; } = string.Empty;
        
        public bool EsCorrecta { get; set; }
        
        [Range(0, int.MaxValue)]
        public int PuntosObtenidos { get; set; }
    }
}