using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LMSBackend.API.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;

        public GoogleDriveService(IConfiguration config)
        {
            var credential = GoogleCredential
                .FromFile("google-drive-credentials.json")
                .CreateScoped(DriveService.Scope.Drive);

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "LMSBackend"
            });

            _folderId = config["GoogleDrive:FolderId"] ?? throw new ArgumentNullException("FolderId not set");
        }

        public async Task<(string FileId, string WebViewLink)> SubirArchivoAsync(IFormFile archivo, string nombreFinal)
        {
            // Validación básica
            var extPermitidas = new[] { ".pdf", ".docx", ".pptx" };
            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (!extPermitidas.Contains(ext))
                throw new Exception("Extensión no permitida");

            // Configurar metadata del archivo
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = nombreFinal,
                Parents = new List<string> { _folderId }
            };

            using var stream = archivo.OpenReadStream();
            var request = _driveService.Files.Create(fileMetadata, stream, archivo.ContentType);
            request.Fields = "id, webViewLink";

            var result = await request.UploadAsync();
            if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new Exception("Error al subir archivo a Drive");

            var uploadedFile = request.ResponseBody;

            return (uploadedFile.Id, uploadedFile.WebViewLink);
        }
        public async Task EliminarArchivoAsync(string fileId)
        {
            await _driveService.Files.Delete(fileId).ExecuteAsync();
        }
        public async Task<(string fileId, string fileUrl)> SubirArchivoAvatarAsync(string userId, IFormFile archivo)
        {
            var extPermitidas = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (!extPermitidas.Contains(ext))
                throw new Exception("Extensión de imagen no permitida");

            string mimeType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"{userId}_avatar{ext}",
                Parents = new List<string> { _folderId }
            };

            using var stream = archivo.OpenReadStream();
            var request = _driveService.Files.Create(fileMetadata, stream, archivo.ContentType);
            request.Fields = "id, webViewLink";


            var result = await request.UploadAsync();
            if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                Console.WriteLine($"[GoogleDrive] Falló la subida del avatar");
                Console.WriteLine($"   Estado: {result.Status}");
                Console.WriteLine($"   Archivo: {archivo.FileName}");
                Console.WriteLine($"   ContentType: {archivo.ContentType}");
                Console.WriteLine($"   FolderId: {_folderId}");

                if (result.Exception != null)
                {
                    Console.WriteLine($"   Excepción detallada: {result.Exception}");
                }
                throw new Exception($"Error al subir avatar a Drive. Estado: {result.Status}");
            }

            var uploadedFile = request.ResponseBody;
            return (uploadedFile.Id, uploadedFile.WebViewLink);
        }
    }
}
