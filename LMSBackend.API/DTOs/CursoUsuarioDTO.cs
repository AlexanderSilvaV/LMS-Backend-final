namespace LMSBackend.API.DTOs
{
    public class CursoUsuarioDTO
    {
        public int CursoId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public string RolEnCurso { get; set; } = string.Empty;
    }
}
