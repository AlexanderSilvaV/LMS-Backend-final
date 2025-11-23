

namespace LMSBackend.API.DTOs // Declarando un espacio logico
{
    public class UsuarioCreacionDTO // Declarando la clase UsuarioCreacionDT, con campos necesarios para crear un usuario.
    {
        // Declaracion de atributos necesitados que el cliente debe enviar

        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Contraseña { get; set; }
        public string? Rol { get; set; }
        public string? Rut { get; set; }
    }
}
