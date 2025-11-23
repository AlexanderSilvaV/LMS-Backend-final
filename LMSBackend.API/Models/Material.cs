using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Models
{
    public enum TipoMaterial
    {
        Archivo,
        Enlace,
        Video
    }
    public class Material
    {
        [Key]
        public int MaterialId { get; set; }
        [MaxLength(70)]
        [Required]
    public string Nombre { get; set; } = string.Empty;
        [Required]
        public TipoMaterial Tipo { get; set; }
        [Required]
        [MaxLength(500)]
    public string Ruta { get; set; } = string.Empty;
        public int ModuloId { get; set; }
    public Modulo Modulo { get; set; } = null!;
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;
    }
}
