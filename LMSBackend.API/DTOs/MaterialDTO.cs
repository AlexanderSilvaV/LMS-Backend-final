namespace LMSBackend.API.DTOs
{
    public class MaterialDTO
    {
        public int MaterialId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;  // Se puede usar string para facilitar consumo en frontend
        public int ModuloId { get; set; }
    }
}
