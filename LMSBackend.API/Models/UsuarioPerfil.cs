using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSBackend.API.Models
{
    public class UsuarioPerfil
    {
        // Relación (1:1)
        [Key]
        [ForeignKey(nameof(Usuario))]
        [Required]
        public string UsuarioId { get; set; } = default!;

        public Usuario Usuario { get; set; } = default!;


        // Editable por el propio Usuario
        [MaxLength(1000)]
        public string? Descripcion { get; set; }

        // Control de concurrencia optimista
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Campo byte para guardado de avatar (deprecated - usar S3)
        public byte[]? AvatarBytes { get; set; }

        // S3 Storage fields
        [MaxLength(255)]
        public string? AvatarFileId { get; set; }

        [MaxLength(2048)]
        public string? AvatarUrl { get; set; }
    }

}
