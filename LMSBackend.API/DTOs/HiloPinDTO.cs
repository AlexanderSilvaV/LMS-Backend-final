
using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.DTOs
{
    public class HiloPinDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "PinnedOrder debe ser >= 1.")]
        public int PinnedOrder { get; set; }
    }
}
