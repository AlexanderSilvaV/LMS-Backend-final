using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using System.Security.Claims;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Solo usuarios autenticados
    public class ProfileController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public ProfileController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPut("me")]
        public async Task<IActionResult> ActualizarMiPerfil([FromBody] UsuarioPerfilEdicionDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo identificar al usuario.");
            }

            var resultado = await _usuarioService.ActualizarDescripcionAsync(userId, dto.Descripcion);
            if (!resultado.OperacionExitosa)
            {
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            }

            return Ok(new { mensaje = resultado.Mensaje });
        }
        [HttpGet("me")]
        public async Task<IActionResult> ObtenerMiPerfil()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo indentificar al usuario");
            }
            var resultado = await _usuarioService.ObtenerPerfilCompletoAsync(userId);
            if (!resultado.OperacionExitosa)
            {
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            }
            return Ok(resultado.Dato);
        }
        [HttpPost("me/avatar")]
        public async Task<IActionResult> SubirAvatar(IFormFile archivo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo identificar al usuario.");
            }

            var resultado = await _usuarioService.ActualizarAvatarAsync(userId, archivo);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(resultado.Dato);
        }

        [HttpDelete("me/avatar")]
        public async Task<IActionResult> EliminarAvatar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo identificar al usuario.");
            }

            var resultado = await _usuarioService.EliminarAvatarAsync(userId);

            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(new { mensaje = resultado.Mensaje });
        }
        [HttpGet("me/avatar")]
        public async Task<IActionResult> ObtenerAvatar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("No se pudo identificar al usuario.");
            }

            var resultado = await _usuarioService.ObtenerUrlAvatarAsync(userId);

            if (!resultado.OperacionExitosa)
            {
                return NotFound("El usuario no tiene avatar.");
            }

            // Retornar la URL de S3 en JSON para que el cliente pueda acceder directamente
            return Ok(new { url = resultado.Dato });
        }
    }
}
