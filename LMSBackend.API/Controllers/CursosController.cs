using System;
using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Services;
using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using LMSBackend.API.Helpers;
using Microsoft.AspNetCore.Http;

namespace LMSBackend.API.Controllers
{
    [ApiController] // Declaro que es una API
    [Route("api/[controller]")] // Declaro la forma en como se puede acceder a la API desde el exterior.

    public class CursosController : ControllerBase
    {
        private readonly CursoService _cursoService;
        private readonly CursoUsuarioService _cursoUsuarioService;
        public CursosController(CursoService cursoService, CursoUsuarioService cursoUsuarioService)
        {
            _cursoService = cursoService;
            _cursoUsuarioService = cursoUsuarioService;
        }
        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<ActionResult<ResultadoOperacion<CursoDTO>>> CrearCurso([FromBody] CursoCreacionDTO dto)
        {

            var resultado = await _cursoService.CrearCursoAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }
        [Authorize(Roles = "Administrador")]
        [HttpPost("buscar")]
        public async Task<ActionResult<ResultadoOperacion<CursoBusquedaDTO>>> BuscarCurso([FromBody] CursoBusquedaDTO dto)
        {
            var resultado = await _cursoService.BuscarCursosAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }
        [HttpPut("{nrc}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ResultadoOperacion<CursoDTO>>> EditarCurso(int nrc, [FromBody] CursoEdicionDTO dto)
        {
            // Llama al servicio para editar el curso con el NRC especificado.
            var resultado = await _cursoService.EditarCursoAsync(nrc, dto);

            // Devuelve la respuesta con el código HTTP correspondiente.
            return StatusCode(resultado.Codigo, resultado);
        }
        [Authorize(Roles = "Administrador")]
        [HttpDelete("{nrc}")]
        public async Task<ActionResult<ResultadoOperacion<string>>> EliminarCurso(int nrc)
        {
            var resultado = await _cursoService.EliminarCursoAsync(nrc);
            return StatusCode(resultado.Codigo, resultado);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpPost("{nrc}/portada")]
        public async Task<IActionResult> SubirPortada(int nrc, [FromForm] IFormFile? archivo)
        {
            if (archivo == null)
            {
                return BadRequest(new { mensaje = "El archivo es requerido" });
            }

            var resultado = await _cursoService.ActualizarPortadaAsync(nrc, archivo);
            return StatusCode(resultado.Codigo, resultado);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpDelete("{nrc}/portada")]
        public async Task<IActionResult> EliminarPortada(int nrc)
        {
            var resultado = await _cursoService.EliminarPortadaAsync(nrc);
            return StatusCode(resultado.Codigo, resultado);
        }

        [AllowAnonymous]
        [HttpGet("{nrc}/portada")]
        public async Task<IActionResult> ObtenerPortada(int nrc)
        {
            var resultado = await _cursoService.ObtenerPortadaAsync(nrc);

            if (!resultado.OperacionExitosa)
            {
                return StatusCode(resultado.Codigo, new { mensaje = resultado.Mensaje });
            }

            var dto = resultado.Dato;

            Response.Headers["Cache-Control"] = "public, max-age=3600";
            if (dto.ActualizadaEn != default)
            {
                Response.Headers["Last-Modified"] = dto.ActualizadaEn.ToUniversalTime().ToString("R");
            }

            if (string.IsNullOrWhiteSpace(dto.Url))
            {
                return NotFound(new { mensaje = "Portada no disponible" });
            }

            return Redirect(dto.Url);
        }
        [HttpGet("asignados")]
        [Authorize(Roles = "Administrador,Docente,Alumno,Profesor")]
        public async Task<ActionResult<ResultadoOperacion<List<CursoDTO>>>> ObtenerCursosAsignados()
        {
            var resultado = await _cursoUsuarioService.ObtenerCursosDelUsuarioActualAsync();
            return StatusCode(resultado.Codigo, resultado);
        }

        // Endpoint para duplicar cursos - Agregado para resolver error "Error sin mensaje del backend"
        // El frontend llamaba a este endpoint pero no existía en el backend, causando 404 errors
        // Ahora el endpoint está disponible y utiliza el servicio DuplicarCursoAsync existente
        // Nicolas Cheuque
        [Authorize(Roles = "Administrador")]
        [HttpPost("duplicar")]
        public async Task<ActionResult<ResultadoOperacion<CursoDTO>>> DuplicarCurso([FromBody] CursoDuplicacionDTO dto)
        {
            var resultado = await _cursoService.DuplicarCursoAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        // ENDPOINTS ALTERNATIVOS EN INGLÉS PARA COMPATIBILIDAD CON FRONTEND
        // Rutas relativas: /api/cursos/{nrc}/cover
        [Authorize(Roles = "Administrador,Docente")]
        [HttpPost("{nrc}/cover")]
        public async Task<IActionResult> SubirPortadaEn(int nrc, [FromForm] IFormFile? archivo)
        {
            return await SubirPortada(nrc, archivo);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpDelete("{nrc}/cover")]
        public async Task<IActionResult> EliminarPortadaEn(int nrc)
        {
            return await EliminarPortada(nrc);
        }

        [AllowAnonymous]
        [HttpGet("{nrc}/cover")]
        public async Task<IActionResult> ObtenerPortadaEn(int nrc)
        {
            return await ObtenerPortada(nrc);
        }

        // Rutas absolutas en inglés: /api/courses/{nrc}/cover
        [Authorize(Roles = "Administrador,Docente")]
        [HttpPost("/api/courses/{nrc}/cover")]
        public async Task<IActionResult> SubirCoverCourses(int nrc, [FromForm] IFormFile? archivo)
        {
            return await SubirPortada(nrc, archivo);
        }

        [Authorize(Roles = "Administrador,Docente")]
        [HttpDelete("/api/courses/{nrc}/cover")]
        public async Task<IActionResult> EliminarCoverCourses(int nrc)
        {
            return await EliminarPortada(nrc);
        }

        [AllowAnonymous]
        [HttpGet("/api/courses/{nrc}/cover")]
        public async Task<IActionResult> ObtenerCoverCourses(int nrc)
        {
            return await ObtenerPortada(nrc);
        }
    }
}
