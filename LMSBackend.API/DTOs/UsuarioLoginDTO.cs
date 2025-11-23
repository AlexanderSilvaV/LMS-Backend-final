
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs // Declarando espacio logico
{
    public class UsuarioLoginDTO // Creando la clase UsuarioLoginDTO
    {
        // Declaración de atributos que el cliente necesita enviar para iniciar sesion
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string? Contraseña { get; set; }
    }
}
