using LMSBackend.API.Configuration;
using LMSBackend.API.DTOs;
using LMSBackend.API.Helpers;
using LMSBackend.API.Models;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LMSBackend.API.Controllers
{
    [ApiController]
    [Route("api/v2/3dlab")]
    public class Laboratorio3DLabController : ControllerBase
    {
        public const string ApiKeyHeaderName = "X-3DLAB-Key";

        private readonly Laboratorio3DLabService _laboratorioService;
        private readonly ThreeDLabOptions _options;
        private readonly ILogger<Laboratorio3DLabController> _logger;
    private readonly UserManager<Usuario> _userManager;
    private readonly TokenService _tokenService;

        public Laboratorio3DLabController(
            Laboratorio3DLabService laboratorioService,
            IOptions<ThreeDLabOptions> options,
            ILogger<Laboratorio3DLabController> logger,
            UserManager<Usuario> userManager,
            TokenService tokenService)
        {
            _laboratorioService = laboratorioService;
            _options = options.Value;
            _logger = logger;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpGet("preguntas")]
        public async Task<IActionResult> ObtenerPreguntas([FromQuery] int evaluacionId)
        {
            var authResult = Autorizar();
            if (authResult != null)
            {
                return authResult;
            }

            if (evaluacionId <= 0)
            {
                return BadRequest("Debe indicar un evaluacionId válido");
            }

            var resultado = await _laboratorioService.ObtenerPoolPreguntasAsync(evaluacionId);
            return FromResultado(resultado);
        }

        [HttpGet("evaluaciones/{evaluacionId:int}/seleccion")]
        public async Task<IActionResult> ObtenerSeleccion(int evaluacionId, [FromQuery] string alumnoId)
        {
            var authResult = Autorizar();
            if (authResult != null)
            {
                return authResult;
            }

            var resultado = await _laboratorioService.CrearSesionYSeleccionarPreguntasAsync(evaluacionId, alumnoId);
            return FromResultado(resultado);
        }

        [HttpPost("resultados")]
        public async Task<IActionResult> RegistrarResultados([FromBody] LaboratorioResultadosRequestDTO request)
        {
            var authResult = Autorizar();
            if (authResult != null)
            {
                return authResult;
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var resultado = await _laboratorioService.RegistrarResultadosAsync(request);
            return FromResultado(resultado);
        }

        [HttpPost("token")]
        public async Task<IActionResult> ObtenerTokenAlumno()
        {
            var authResult = Autorizar();
            if (authResult != null)
            {
                return authResult;
            }

            if (string.IsNullOrWhiteSpace(_options.ServiceUserEmail))
            {
                _logger.LogError("3DLAB: No se pudo generar token porque ServiceUserEmail no está configurado");
                return StatusCode(500, new { mensaje = "Configuración incompleta para usuario de integración" });
            }

            var usuario = await _userManager.FindByEmailAsync(_options.ServiceUserEmail.Trim());
            if (usuario == null)
            {
                _logger.LogError("3DLAB: Usuario de integración {Email} no encontrado", _options.ServiceUserEmail);
                return StatusCode(500, new { mensaje = "Usuario de integración no disponible" });
            }

            if (!string.Equals(usuario.Rol, _options.ServiceUserRole, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "3DLAB: Usuario de integración {Email} tiene rol {RolActual} distinto al esperado {RolEsperado}",
                    _options.ServiceUserEmail,
                    usuario.Rol,
                    _options.ServiceUserRole);
            }

            var token = _tokenService.GenerarToken(usuario);
            return Ok(new { token, rol = usuario.Rol });
        }

        private IActionResult? Autorizar()
        {
            var apiKey = _options.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("API Key 3DLAB no configurada");
                return StatusCode(500, "API Key no configurada");
            }

            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) || !string.Equals(providedKey, apiKey, StringComparison.Ordinal))
            {
                return Unauthorized("Header de autenticación inválido");
            }

            return null;
        }

        private static IActionResult FromResultado<T>(ResultadoOperacion<T> resultado)
        {
            if (resultado.OperacionExitosa)
            {
                return new ObjectResult(resultado.Dato) { StatusCode = resultado.Codigo };
            }

            return new ObjectResult(new { mensaje = resultado.Mensaje }) { StatusCode = resultado.Codigo };
        }
    }
}
