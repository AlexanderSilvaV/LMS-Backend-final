using LMSBackend.API.Models;
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class MaterialCreacionDTO
    {
        [MaxLength(70)]
        public string Nombre { get; set; } = string.Empty;

        public TipoMaterial Tipo { get; set; }

        [MaxLength(900)]
        public string Ruta { get; set; } = string.Empty;

        public int ModuloId { get; set; }
    }
}
