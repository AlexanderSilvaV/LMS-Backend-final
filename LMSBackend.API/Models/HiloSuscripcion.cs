using System;

namespace LMSBackend.API.Models
{
    public class HiloSuscripcion
    {
        public string UsuarioId { get; set; } = string.Empty;
        public int HiloId { get; set; }
        public Hilo Hilo { get; set; } = null!;
        public DateTime FechaSuscripcion { get; set; }

    }
}
