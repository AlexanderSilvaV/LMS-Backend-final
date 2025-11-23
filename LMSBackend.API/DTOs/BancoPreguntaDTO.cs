using System;
using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class BancoPreguntaDTO
    {
        public int Id { get; set; }
        public string Enunciado { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string DocenteId { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int Puntos { get; set; }
        public bool Activa { get; set; }
        public List<OpcionBancoDTO> Opciones { get; set; } = new();
    }
}