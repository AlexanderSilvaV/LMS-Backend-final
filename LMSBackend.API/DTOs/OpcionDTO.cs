namespace LMSBackend.API.DTOs
{
    public class OpcionDTO
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool EsCorrecta { get; set; }
        public int Orden { get; set; }
        public int PreguntaId { get; set; }
    }
}