using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForosController : ControllerBase
    {
        private readonly ForoService _foroService;

        public ForosController(ForoService foroService)
        {
            _foroService = foroService;
        }

        // Crear foro
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ForoCreacionDTO dto)
        {
            var resultado = await _foroService.CrearForoAsync(dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Editar foro
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPut("{foroId:int}")]
        public async Task<IActionResult> Editar(int foroId, [FromBody] ForoEdicionDTO dto)
        {
            var resultado = await _foroService.EditarForoAsync(foroId, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Cambiar estado
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPatch("{foroId:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int foroId, [FromBody] ForoCambioEstadoDTO dto)
        {
            var resultado = await _foroService.CambiarEstadoForoAsync(foroId, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Actualizar políticas del foro
        [Authorize(Roles = "Administrador, Docente")]
        [HttpPatch("{foroId:int}/policies")]
        public async Task<IActionResult> ActualizarPoliticas(int foroId, [FromBody] ForoPoliciesDTO dto)
        {
            var resultado = await _foroService.ActualizarPoliticasForoAsync(foroId, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Obtener foro por id
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        [HttpGet("{foroId:int}")]
        public async Task<IActionResult> Obtener(int foroId)
        {
            var resultado = await _foroService.ObtenerForoAsync(foroId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Listar foros por módulo con filtros/paginación
        [Authorize(Roles = "Administrador, Docente, Alumno")]
        [HttpPost("listar")]
        public async Task<IActionResult> Listar([FromBody] ForoListadoDTO dto)
        {
            var resultado = await _foroService.ListarForosPorModuloAsync(dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        // Eliminar foro
        [Authorize(Roles = "Administrador, Docente")]
        [HttpDelete("{foroId:int}")]
        public async Task<IActionResult> Eliminar(int foroId)
        {
            var resultado = await _foroService.EliminarForoAsync(foroId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Mensaje);
        }
    }
}
