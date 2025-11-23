namespace LMSBackend.API.DTOs
{
    public class OpcionBancoDTO
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool EsCorrecta { get; set; }
        public int Orden { get; set; }
        public int BancoPreguntaId { get; set; }
    }
}