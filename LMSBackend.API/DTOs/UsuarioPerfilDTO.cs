
namespace LMSBackend.API.DTOs
{
    public class UsuarioPerfilDTO
    {
        public string UsuarioId { get; set; } = default!;
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
        public string? AvatarUrl { get; set; }

        public bool TieneAvatar { get; set; }
    }
}
