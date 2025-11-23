using LMSBackend.API.DTOs;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Helpers;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialesController : ControllerBase
    {
        private readonly MaterialService _materialService;

        public MaterialesController(MaterialService materialService)
        {
            _materialService = materialService;
        }

        [Authorize(Roles = "Administrador,Docente,Alumno")]
        [HttpGet("modulo/{moduloId}")]
        public async Task<IActionResult> ObtenerPorModulo(int moduloId)
        {
            var resultado = await _materialService.ObtenerMaterialesPorModuloIdAsync(moduloId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] MaterialCreacionDTO dto)
        {
            var resultado = await _materialService.CrearMaterialAsync(dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpPut("{materialId}")]
        public async Task<IActionResult> Editar(int materialId, [FromBody] MaterialEdicionDTO dto)
        {
            var resultado = await _materialService.EditarMaterialAsync(materialId, dto);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Dato);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpDelete("{materialId}")]
        public async Task<IActionResult> Eliminar(int materialId)
        {
            var resultado = await _materialService.EliminarMaterialAsync(materialId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado.Mensaje);
            return Ok(resultado.Mensaje);
        }
        [HttpPost("modulo/{moduloId}/archivo")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<MaterialDTO>>> SubirArchivo(int moduloId, IFormFile archivo)
        {
            var resultado = await _materialService.CrearMaterialArchivoAsync(moduloId, archivo);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado);
            return Ok(resultado.Dato);
        }
        [HttpPost("{materialId}/reemplazar-archivo")]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<ActionResult<ResultadoOperacion<MaterialDTO>>> ReemplazarArchivo(int materialId, IFormFile? nuevoArchivo)
        {
            var resultado = await _materialService.ReemplazarArchivoAsync(materialId, nuevoArchivo);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, resultado);
            return Ok(resultado.Dato);
        }

        [HttpGet("{materialId}/download")]
        [Authorize(Roles = "Administrador,Docente,Alumno")]
        public async Task<IActionResult> ObtenerUrlDescarga(int materialId)
        {
            var resultado = await _materialService.GenerarUrlDescargaAsync(materialId);
            if (!resultado.OperacionExitosa)
                return StatusCode(resultado.Codigo, new { mensaje = resultado.Mensaje });
            return Ok(new { url = resultado.Dato });
        }
    }
}
