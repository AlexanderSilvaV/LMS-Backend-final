
namespace LMSBackend.API.DTOs
{
    public class ModuloDTO
    {
        public int ModuloId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Indice { get; set; }
        public bool EsPredeterminado { get; set; }
        public int CursoId { get; set; }
    }
}
