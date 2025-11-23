using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace LMSBackend.API.Models
{
    public class Curso
    {
        [Key]
        public int Nrc { get; set; }
        [Required]
        [MaxLength(70)]
    public string Nombre { get; set; } = string.Empty;
        [Required]
        [MaxLength(250)]
    public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    public string AdministradorId { get; set; } = string.Empty;
    public Usuario Administrador { get; set; } = null!;
        public List<Modulo> Modulos { get; set; } = new();
    public ICollection<CursoUsuario> CursoUsuarios { get; set; } = new List<CursoUsuario>();

        [MaxLength(150)]
        public string? PortadaFileId { get; set; }
        [MaxLength(500)]
        public string? PortadaUrl { get; set; }
        public DateTime? PortadaActualizada { get; set; }
    }
}
