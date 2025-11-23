using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Services;
using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace LMSBackend.API.Controllers
{
    [ApiController] // Declaro que es una API
    [Route("api/[controller]")] // Declaro la forma en como se puede acceder a la API desde el exterior.

    public class UsuariosController : ControllerBase
    {
        private readonly UsuarioService _usuarioService; // Declarando metodo UsuarioService, privado y solo lectura.

        public UsuariosController(UsuarioService usuarioService) // Inyectando el metodo a la clase UsuriosController.
        {
            _usuarioService = usuarioService;
        }
        [HttpPost] // Metodo Responde a solo solicitudes Post
        public async Task<ActionResult> CrearUsuario([FromBody] UsuarioCreacionDTO dto) // Metodo público y asincrónico que maneja la creación de nuevos usuarios.
                                                                                        // Recibe un DTO desde el cuerpo de la soliciutd y retonra un resultado HTTP.`
        {
            var resultado = await _usuarioService.CrearUsuarioAsync(dto);

            if (resultado.OperacionExitosa)
            {
                return Ok(resultado.Mensaje);
            }
            else
            {
                return BadRequest(resultado.Mensaje);
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] UsuarioLoginDTO dto)
        {
            try
            {
                var resultado = await _usuarioService.LoginUsuarioAsync(dto);

                if (resultado.OperacionExitosa)
                {
                    return Ok(new { token = resultado.Dato.Token });
                }
                else
                {
                    return StatusCode(resultado.Codigo, resultado.Mensaje);
                }
            }
            catch (Exception)
            {
                // Log the exception here if needed
                return StatusCode(500, "Error interno del servidor. Inténtalo más tarde.");
            }
        }
        [HttpPost("recuperar-password")]
        public async Task<ActionResult> RecuperarPassword([FromBody] RecuperarPasswordDTO dto)
        {
            var resultado = await _usuarioService.RecuperarPasswordAsync(dto);

            if (resultado.OperacionExitosa)
            {
                return Ok(new { mensaje = resultado.Mensaje });
            }
            else
            {
                return StatusCode(resultado.Codigo, new { mensaje = resultado.Mensaje });
            }
        }
        [HttpPost("restablecer-password")]
        public async Task<ActionResult> RestablecerPassword([FromBody] RestablecerPasswordDTO dto)
        {
            var resultado = await _usuarioService.RestablecerPasswordAsync(dto);

            if (resultado.OperacionExitosa)
            {
                return Ok(resultado.Mensaje);
            }
            else
            {
                return BadRequest(resultado.Mensaje);
            }
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult> Profile()
        {
            var resultado = await _usuarioService.ObtenerPerfilAsync();

            if (resultado.OperacionExitosa)
            {
                return Ok(resultado);
            }
            return Unauthorized(resultado);
        }
        [HttpGet]
        [Authorize(Roles = "Administrador, Alumno")]
        public async Task<IActionResult> GetUsuarios([FromQuery] PaginacionDTO dto)
        {
            var resultado = await _usuarioService.ListarUsuariosAsync(dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(new
            {
                Usuarios = resultado.Dato,
                Paginacion = dto
            });
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetUsuarioPorId(string id)
        {
            var resultado = await _usuarioService.ObtenerUsuarioPorIdAsync(id);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        [HttpGet("{id}/public")]
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        public async Task<IActionResult> GetUsuarioPublico(string id)
        {
            Console.WriteLine($"🔍 [Backend] GetUsuarioPublico called for ID: {id}");
            var resultado = await _usuarioService.ObtenerUsuarioPorIdAsync(id);
            Console.WriteLine($"📊 [Backend] ObtenerUsuarioPorIdAsync result: {resultado.OperacionExitosa}");
            if (!resultado.OperacionExitosa)
            {
                Console.WriteLine($"❌ [Backend] Error: {resultado.Mensaje}");
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            }
            
            // Obtener el rol del usuario que hace la petición
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            // Los docentes pueden ver el RUT de estudiantes y otros docentes
            if (userRole == "Docente")
            {
                var usuarioPublico = new
                {
                    id = resultado.Dato.Id,
                    nombre = resultado.Dato.Nombre,
                    rut = resultado.Dato.Rut,
                    fotoPerfil = resultado.Dato.FotoPerfil,
                    rol = resultado.Dato.Rol
                };
                Console.WriteLine($"✅ [Backend] Returning user info for docente: ID={usuarioPublico.id}, Nombre={usuarioPublico.nombre}, RUT={usuarioPublico.rut}");
                return Ok(usuarioPublico);
            }
            else
            {
                // Información pública básica para alumnos y otros
                var usuarioPublico = new
                {
                    id = resultado.Dato.Id,
                    nombre = resultado.Dato.Nombre,
                    fotoPerfil = resultado.Dato.FotoPerfil,
                    rol = resultado.Dato.Rol
                };
                Console.WriteLine($"✅ [Backend] Returning user info: ID={usuarioPublico.id}, Nombre={usuarioPublico.nombre}, FotoPerfil={(usuarioPublico.fotoPerfil != null ? "Presente" : "Null")}");
                return Ok(usuarioPublico);
            }
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateUsuario(string id, [FromBody] UsuarioEdicionDTO dto)
        {
            var resultado = await _usuarioService.EditarUsuarioAsync(id, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Mensaje);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteUsuario(string id)
        {
            var resultado = await _usuarioService.EliminarUsuarioAsync(id);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Mensaje);
        }
        [HttpPut("{id}/name")]
        public async Task<IActionResult> ActualizarNombre(string id, [FromBody] ActualizarNombreDTO dto)
        {
            var resultado = await _usuarioService.ActualizarNombreAsync(id, dto.Nombre);

            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(new { mensaje = resultado.Mensaje });
        }

        // Endpoint temporal para asignar RUTs de prueba
        [HttpPost("asignar-ruts-prueba")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AsignarRutsDePrueba()
        {
            var resultado = await _usuarioService.AsignarRutsDePruebaAsync();

            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);

            return Ok(new { mensaje = resultado.Mensaje });
        }
    }
}
