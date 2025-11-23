using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LMSBackend.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private static readonly TimeZoneInfo ChileTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Convierte una fecha UTC a la hora de Chile (UTC-3/UTC-4 según horario de verano)
        /// </summary>
        private string FormatearFechaChile(DateTime fechaUtc)
        {
            try
            {
                // Convertir de UTC a hora de Chile
                var fechaChile = TimeZoneInfo.ConvertTimeFromUtc(fechaUtc, ChileTimeZone);
                return fechaChile.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error al convertir fecha a hora de Chile: {ex.Message}. Usando UTC.");
                return fechaUtc.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                var fromEmail = _configuration["SendGrid:From"];
                var fromName = _configuration["SendGrid:FromName"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("SendGrid API Key no configurada");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(destinatario);

                var msg = MailHelper.CreateSingleEmail(from, to, asunto, "", cuerpoHtml);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email enviado exitosamente a {destinatario} via SendGrid");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Error al enviar email via SendGrid a {destinatario}. Status: {response.StatusCode}, Body: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al enviar email a {destinatario}: {ex.Message}");
                // No lanzar excepción para no bloquear operaciones principales
            }
        }

        /// <summary>
        /// Envía notificación cuando se publica una nueva evaluación
        /// </summary>
        public async Task EnviarNotificacionNuevaEvaluacionAsync(
            string destinatario,
            string nombreUsuario,
            string nombreCurso,
            string tituloEvaluacion,
            DateTime fechaInicio,
            DateTime fechaFin,
            string enlaceEvaluacion)
        {
            var asunto = $"Nueva Evaluación: {tituloEvaluacion}";
            var cuerpoHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; margin: 10px 0; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Nueva Evaluación Disponible</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>
                            <p>Se ha publicado una nueva evaluación en el curso <strong>{nombreCurso}</strong>.</p>
                            <h3>{tituloEvaluacion}</h3>
                            <p><strong>Fecha de inicio:</strong> {FormatearFechaChile(fechaInicio)}</p>
                            <p><strong>Fecha de cierre:</strong> {FormatearFechaChile(fechaFin)}</p>
                            <p>
                                <a href='{enlaceEvaluacion}' class='button'>Ir a la Evaluación</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await EnviarCorreoAsync(destinatario, asunto, cuerpoHtml);
        }

        /// <summary>
        /// Envía recordatorio de evaluación pendiente
        /// </summary>
        public async Task EnviarRecordatorioEvaluacionAsync(
            string destinatario,
            string nombreUsuario,
            string nombreCurso,
            string tituloEvaluacion,
            DateTime fechaFin,
            string enlaceEvaluacion)
        {
            var asunto = $"Recordatorio: {tituloEvaluacion}";
            var cuerpoHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ background-color: #FF9800; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; margin: 10px 0; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Recordatorio de Evaluación</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>
                            <div class='warning'>
                                <p><strong>¡Recordatorio!</strong> Tienes una evaluación pendiente en el curso <strong>{nombreCurso}</strong>.</p>
                            </div>
                            <h3>{tituloEvaluacion}</h3>
                            <p><strong>Fecha de cierre:</strong> {FormatearFechaChile(fechaFin)}</p>
                            <p>No olvides completarla antes de la fecha límite.</p>
                            <p>
                                <a href='{enlaceEvaluacion}' class='button'>Ir a la Evaluación</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await EnviarCorreoAsync(destinatario, asunto, cuerpoHtml);
        }

        /// <summary>
        /// Envía notificación cuando se publica un nuevo hilo en el foro
        /// </summary>
        public async Task EnviarNotificacionPublicacionHiloAsync(
            string destinatario,
            string nombreUsuario,
            string nombreCurso,
            string tituloForo,
            string tituloHilo,
            string autorHilo,
            string enlaceHilo)
        {
            var asunto = $"Nuevo Hilo en Foro: {tituloHilo}";
            var cuerpoHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; margin: 10px 0; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Nuevo Hilo en Foro</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>
                            <p>Se ha publicado un nuevo hilo en el foro <strong>{tituloForo}</strong> del curso <strong>{nombreCurso}</strong>.</p>
                            <h3>{tituloHilo}</h3>
                            <p><strong>Autor:</strong> {autorHilo}</p>
                            <p>
                                <a href='{enlaceHilo}' class='button'>Ver Hilo</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await EnviarCorreoAsync(destinatario, asunto, cuerpoHtml);
        }

        /// <summary>
        /// Envía notificación cuando alguien responde a un hilo
        /// </summary>
        public async Task EnviarNotificacionRespuestaHiloAsync(
            string destinatario,
            string nombreUsuario,
            string tituloHilo,
            string autorRespuesta,
            string enlaceHilo)
        {
            var asunto = $"Nueva Respuesta: {tituloHilo}";
            var cuerpoHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .button {{ background-color: #9C27B0; color: white; padding: 10px 20px; text-decoration: none; display: inline-block; margin: 10px 0; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Nueva Respuesta en Hilo</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>
                            <p><strong>{autorRespuesta}</strong> ha respondido en el hilo:</p>
                            <h3>{tituloHilo}</h3>
                            <p>
                                <a href='{enlaceHilo}' class='button'>Ver Respuesta</a>
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await EnviarCorreoAsync(destinatario, asunto, cuerpoHtml);
        }

        /// <summary>
        /// Envía notificación cuando se cierra una evaluación
        /// </summary>
        public async Task EnviarNotificacionCierreEvaluacionAsync(
            string destinatario,
            string nombreUsuario,
            string nombreCurso,
            string tituloEvaluacion,
            DateTime fechaCierre)
        {
            var asunto = $"Evaluación Cerrada: {tituloEvaluacion}";
            var cuerpoHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; background-color: #f9f9f9; }}
                        .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                        .info {{ background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 10px; margin: 10px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Evaluación Cerrada</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>
                            <div class='info'>
                                <p>La evaluación <strong>{tituloEvaluacion}</strong> del curso <strong>{nombreCurso}</strong> ha sido cerrada.</p>
                            </div>
                            <p><strong>Fecha de cierre:</strong> {FormatearFechaChile(fechaCierre)}</p>
                            <p>Pronto se publicarán las calificaciones.</p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await EnviarCorreoAsync(destinatario, asunto, cuerpoHtml);
        }

        /// <summary>
        /// Envía email de recuperación de contraseña usando SendGrid
        /// </summary>
        public async Task EnviarRecuperacionPasswordAsync(
            string destinatario,
            string nombreUsuario,
            string enlaceRecuperacion)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                var fromEmail = _configuration["SendGrid:From"];
                var fromName = _configuration["SendGrid:FromName"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("SendGrid API Key no configurada");
                    throw new Exception("Configuración de SendGrid incompleta");
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(destinatario, nombreUsuario);
                var subject = "Recuperación de Contraseña - LMS";

                var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }}
                        .header h1 {{ margin: 0; font-size: 28px; }}
                        .content {{ padding: 40px 30px; }}
                        .content p {{ margin: 15px 0; font-size: 16px; }}
                        .button-container {{ text-align: center; margin: 30px 0; }}
                        .button {{
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white;
                            padding: 15px 40px;
                            text-decoration: none;
                            display: inline-block;
                            border-radius: 5px;
                            font-weight: bold;
                            font-size: 16px;
                            box-shadow: 0 4px 6px rgba(102, 126, 234, 0.4);
                            transition: all 0.3s ease;
                        }}
                        .button:hover {{ box-shadow: 0 6px 8px rgba(102, 126, 234, 0.6); }}
                        .warning {{
                            background-color: #fff3cd;
                            border-left: 4px solid #ffc107;
                            padding: 15px;
                            margin: 20px 0;
                            border-radius: 4px;
                        }}
                        .warning strong {{ color: #856404; }}
                        .footer {{
                            background-color: #f8f9fa;
                            text-align: center;
                            padding: 20px;
                            font-size: 12px;
                            color: #666;
                            border-top: 1px solid #dee2e6;
                        }}
                        .logo {{ font-size: 36px; margin-bottom: 10px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <div class='logo'>🔐</div>
                            <h1>Recuperación de Contraseña</h1>
                        </div>
                        <div class='content'>
                            <p>Hola <strong>{nombreUsuario}</strong>,</p>

                            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en el sistema LMS.</p>

                            <p>Si solicitaste este cambio, haz clic en el siguiente botón para crear una nueva contraseña:</p>

                            <div class='button-container'>
                                <a href='{enlaceRecuperacion}' class='button'>Restablecer Contraseña</a>
                            </div>

                            <div class='warning'>
                                <p><strong>⚠️ Importante:</strong></p>
                                <ul style='margin: 10px 0; padding-left: 20px;'>
                                    <li>Este enlace expirará en <strong>24 horas</strong> por razones de seguridad</li>
                                    <li>Si no solicitaste este cambio, ignora este correo y tu contraseña permanecerá sin cambios</li>
                                    <li>Nunca compartas este enlace con nadie</li>
                                </ul>
                            </div>

                            <p>Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:</p>
                            <p style='word-break: break-all; color: #667eea; font-size: 14px;'>{enlaceRecuperacion}</p>

                            <p style='margin-top: 30px;'>Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarnos.</p>

                            <p>Saludos,<br><strong>Equipo LMS</strong></p>
                        </div>
                        <div class='footer'>
                            <p>Este es un correo automático del sistema LMS. Por favor no responder.</p>
                            <p>&copy; 2025 Sistema LMS - Universidad Andrés Bello</p>
                        </div>
                    </div>
                </body>
                </html>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email de recuperación enviado exitosamente a {destinatario} via SendGrid");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"Error al enviar email via SendGrid a {destinatario}. Status: {response.StatusCode}, Body: {responseBody}");
                    throw new Exception($"Error al enviar email: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción al enviar email de recuperación a {destinatario}: {ex.Message}");
                throw;
            }
        }
    }
}
