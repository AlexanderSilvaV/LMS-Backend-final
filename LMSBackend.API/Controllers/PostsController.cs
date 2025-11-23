using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador, Docente, Alumno")]
    public class PostsController : ControllerBase
    {
        private readonly PostService _postService;

        public PostsController(PostService postService)
        {
            _postService = postService;
        }

        // Crear post en un hilo
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PostCreacionDTO dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _postService.CrearPostAsync(dto.HiloId, userId, dto.Contenido);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Editar post (solo autor o moderador; lo valida el service)
        [HttpPut("{postId:int}")]
        public async Task<IActionResult> Editar(int postId, [FromBody] PostEdicionDTO dto)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _postService.EditarPostAsync(postId, userId, dto.Contenido);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Borrar post (soft-delete; autor o moderador)
        [HttpDelete("{postId:int}")]
        public async Task<IActionResult> Borrar(int postId)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _postService.BorrarPostAsync(postId, userId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        // Listar posts de un hilo (aplica gating RequireInitialPostToView en el service)
        [HttpPost("{hiloId:int}/listar")]
        public async Task<IActionResult> Listar(int hiloId, [FromBody] PostListadoDTO dto)
        {
            var viewerId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(viewerId))
                return StatusCode(401, "Usuario no autenticado.");

            var resultado = await _postService.ListarPostsAsync(hiloId, dto.Pagina, dto.CantidadPorPagina, viewerId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }
    }
}
