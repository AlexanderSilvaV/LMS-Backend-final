namespace LMSBackend.API.DTOs
{
    public class NotaDTO
    {
        public int Id { get; set; }
        public decimal Calificacion { get; set; }
        public string? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioRut { get; set; }
        public int? EvaluacionId { get; set; }
        public string? EvaluacionTitulo { get; set; }
        public DateTime? FechaCalificacion { get; set; }
        public string? Observaciones { get; set; }
        public int? NumeroIntento { get; set; }
        public bool EsNotaFinal { get; set; }
    }
}