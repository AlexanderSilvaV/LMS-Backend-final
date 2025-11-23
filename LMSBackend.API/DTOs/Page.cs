using System.Collections.Generic;

namespace LMSBackend.API.DTOs
{
    // Usado por ListByModuloAsync<ForoListItemDTO>
    public class Page<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public List<T> Items { get; set; } = new();
    }
}
