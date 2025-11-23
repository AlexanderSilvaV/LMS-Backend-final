using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    /// <summary>
    /// Body para actualizar políticas del foro.
    /// Ambos campos son opcionales: si vienen con valor, se aplican.
    /// </summary>
    public class ForoPoliciesDTO
    {
        public bool? AllowStudentThreads { get; set; }
        public bool? RequireInitialPostToView { get; set; }
    }
}
