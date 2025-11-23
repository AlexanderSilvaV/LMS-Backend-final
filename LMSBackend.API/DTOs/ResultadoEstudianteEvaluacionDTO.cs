namespace LMSBackend.API.DTOs
{
    public class ResultadoEstudianteEvaluacionDTO
    {
        public string? UsuarioId { get; set; }
        public string? UsuarioRut { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Correo { get; set; }
        public decimal? Nota { get; set; }
        public double? Porcentaje { get; set; }
        public DateTime? FechaRealizacion { get; set; }
        public int Intentos { get; set; }
        public bool Completada { get; set; }
    }
}