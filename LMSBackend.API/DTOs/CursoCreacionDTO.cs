
namespace LMSBackend.API.DTOs
{
    public class CursoCreacionDTO
    {
        public int Nrc { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
