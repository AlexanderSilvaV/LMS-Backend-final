using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace LMSBackend.API.Services
{
    public interface IGoogleDriveService
    {
        Task<(string fileId, string fileUrl)> SubirArchivoAvatarAsync(string userId, IFormFile archivo);
        Task EliminarArchivoAsync(string fileId);
    }
}
