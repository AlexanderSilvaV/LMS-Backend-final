
namespace LMSBackend.API.DTOs
{
    public class UsuarioProfileDTO // DTO utilizado para retornar la info basica del perfil del usuario autenticado
    {
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
