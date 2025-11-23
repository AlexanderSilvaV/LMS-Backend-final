namespace LMSBackend.API.DTOs
{
    public class AsignacionUsuarioCursoDTO
    {
        public int CursoId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string RolEnCurso { get; set; } = string.Empty; // Validaremos contra el enum
    }
}
