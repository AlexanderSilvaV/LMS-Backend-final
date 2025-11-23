using Microsoft.AspNetCore.Mvc;
using LMSBackend.API.Services;
using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using LMSBackend.API.Helpers;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BancoPreguntasController : ControllerBase
    {
        private readonly BancoPreguntasService _bancoPreguntasService;
        private readonly ILogger<BancoPreguntasController> _logger;

        public BancoPreguntasController(BancoPreguntasService bancoPreguntasService, ILogger<BancoPreguntasController> logger)
        {
            _bancoPreguntasService = bancoPreguntasService;
            _logger = logger;
        }

        /// <summary>
        /// Agregar una nueva pregunta al banco de preguntas
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<BancoPreguntaDTO>>> AgregarPregunta([FromBody] BancoPreguntaCreacionDTO dto)
        {
            var resultado = await _bancoPreguntasService.AgregarPreguntaAsync(dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Actualizar una pregunta existente en el banco
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<BancoPreguntaDTO>>> ActualizarPregunta(int id, [FromBody] BancoPreguntaEdicionDTO dto)
        {
            var resultado = await _bancoPreguntasService.ActualizarPreguntaAsync(id, dto);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Eliminar una pregunta del banco
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<bool>>> EliminarPregunta(int id)
        {
            var resultado = await _bancoPreguntasService.EliminarPreguntaAsync(id);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Listar preguntas del banco con paginación y filtros
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<Page<BancoPreguntaDTO>>>> ListarPreguntas(
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanoPagina = 10,
            [FromQuery] string? categoria = null)
        {
            var paginacion = new PaginacionDTO
            {
                PaginaActual = numeroPagina,
                CantidadPorPagina = tamanoPagina
            };

            var resultado = await _bancoPreguntasService.ListarPreguntasAsync(paginacion, categoria);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Obtener todas las categorías disponibles
        /// </summary>
        [HttpGet("categorias")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<List<string>>>> ObtenerCategorias()
        {
            var resultado = await _bancoPreguntasService.ObtenerCategoriasAsync();
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Agregar una nueva categoría al banco de preguntas
        /// </summary>
        [HttpPost("categorias")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<string>>> AgregarCategoria([FromBody] string categoria)
        {
            var resultado = await _bancoPreguntasService.AgregarCategoriaAsync(categoria);
            return StatusCode(resultado.Codigo, resultado);
        }

        /// <summary>
        /// Importar preguntas desde un archivo Excel
        /// </summary>
        [HttpPost("importar")]
        [Authorize(Roles = "Administrador,Docente")]
        public async Task<ActionResult<ResultadoOperacion<ImportacionExcelDTO>>> ImportarDesdeExcel(IFormFile archivo)
        {
            var resultado = await _bancoPreguntasService.ImportarDesdeExcelAsync(archivo);
            return StatusCode(resultado.Codigo, resultado);
        }
    }
}