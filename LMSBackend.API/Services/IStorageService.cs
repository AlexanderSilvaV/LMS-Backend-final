using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LMSBackend.API.DTOs;

namespace LMSBackend.API.Services
{
    /// <summary>
    /// Contrato para servicios de almacenamiento de archivos (local, nube, etc.)
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Guarda un archivo en el almacenamiento configurado.
        /// </summary>
        /// <param name="userId">Id del usuario (puede usarse en el nombre o subcarpeta).</param>
        /// <param name="archivo">Archivo recibido desde el cliente.</param>
        /// <param name="subfolder">Subcarpeta lógica (ej: "avatars", "materials").</param>
        /// <returns>Tupla con fileId (nombre único interno) y fileUrl (URL para acceder o descargar el archivo).</returns>
        Task<(string fileId, string fileUrl)> SaveFileAsync(string userId, IFormFile archivo, string subfolder);

        /// <summary>
        /// Elimina un archivo específico del almacenamiento.
        /// </summary>
        /// <param name="fileId">Identificador único del archivo (generalmente el nombre con GUID).</param>
        /// <param name="subfolder">Subcarpeta donde está almacenado.</param>
        Task DeleteFileAsync(string fileId, string subfolder);

        /// <summary>
        /// Copia un archivo existente creando una nueva copia con un ID único.
        /// </summary>
        /// <param name="originalFileId">ID del archivo original a copiar.</param>
        /// <param name="subfolder">Subcarpeta donde está almacenado el archivo original.</param>
        /// <returns>Tupla con el nuevo fileId y fileUrl de la copia.</returns>
        Task<(string fileId, string fileUrl)> CopyFileAsync(string originalFileId, string subfolder);

        /// <summary>
        /// Genera una URL prefirmada para descargar un archivo (usado principalmente en S3).
        /// </summary>
        /// <param name="userId">Id del usuario propietario del archivo.</param>
        /// <param name="request">Datos de solicitud con fileId, subfolder y nombre de descarga opcional.</param>
        /// <returns>Respuesta con la URL prefirmada para descarga.</returns>
        Task<GenerateDownloadUrlResponseDTO> GeneratePresignedDownloadUrlAsync(string userId, GenerateDownloadUrlRequestDTO request);
    }
}
