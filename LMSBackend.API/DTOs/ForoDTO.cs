using System;

namespace LMSBackend.API.DTOs
{
    public class ForoDTO
    {
        public int ForoId { get; set; }
        public int ModuloId { get; set; }

        public required string Titulo { get; set; }
        public string? Descripcion { get; set; }

        // En el DTO lo exponemos como string para desacoplar del enum interno
        public required string Estado { get; set; }

        public bool AllowStudentThreads { get; set; }
        public bool RequireInitialPostToView { get; set; }

        public required string CreadorId { get; set; }
        public string? CreadorNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
