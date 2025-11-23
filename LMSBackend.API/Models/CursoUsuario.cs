
namespace LMSBackend.API.Models
{
    public class CursoUsuario
    {
        public string UsuarioId { get; set; } = string.Empty;
        public int CursoId { get; set; }
        public RolEnCurso RolEnCurso { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Curso Curso { get; set; } = null!;
    }

    public enum RolEnCurso
    {
        Alumno,
        Docente
    }
}
