using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace LMSBackend.API.Models //Declarando un spacio logico, para organizar el codigo y solucionar problemas de nombramiento de clases.
{
    public class Usuario : IdentityUser // Declarando clase usuario, heredando de IdentityUser
    {
        public string Nombre { get; set; } = string.Empty; // Agregando un campo personalizado, que es propiedad publica con getter y setter (puedes leer y escribir).
        public string Rut { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public ICollection<CursoUsuario> CursoUsuarios { get; set; } = new List<CursoUsuario>();
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Establecer Relacion con Perfil
        public UsuarioPerfil? Perfil { get; set; }
    }
}
