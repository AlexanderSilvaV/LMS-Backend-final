
namespace LMSBackend.API.DTOs
{
    public class RestablecerPasswordDTO
    {
        public string Correo { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NuevaContraseña { get; set; } = string.Empty;
    }
}
