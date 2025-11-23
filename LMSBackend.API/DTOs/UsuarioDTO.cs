using System;

namespace LMSBackend.API.DTOs
{
    public class UsuarioDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string? FotoPerfil { get; set; }
    }
}
