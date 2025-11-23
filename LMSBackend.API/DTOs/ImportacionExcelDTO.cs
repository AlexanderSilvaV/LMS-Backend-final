using System;
using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    public class ImportacionExcelDTO
    {
        public string FileName { get; set; } = string.Empty;
        public int CantidadPreguntasImportadas { get; set; }
        public int CantidadPreguntasConErrores { get; set; }
        public List<string> Errores { get; set; } = new();
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaImportacion { get; set; } = DateTime.UtcNow;
    }
}