using LMSBackend.API.DTOs;
using Microsoft.AspNetCore.Identity;
using LMSBackend.API.Models;
using LMSBackend.API.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using LMSBackend.API.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace LMSBackend.API.Services
{

    public class UsuarioService
    {
        private readonly UserManager<Usuario> _userManager; // Inyección de dependencia para gestionar usuarios mediante ASP.NET Identity (solo lectura).
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IStorageService _storageService;

        public UsuarioService(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager, TokenService tokenService, EmailService emailService, IHttpContextAccessor httpContextAccessor, ILogger<UsuarioService> logger, IStorageService storageService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _storageService = storageService;
        }
        public async Task<ResultadoOperacion<string>> CrearUsuarioAsync(UsuarioCreacionDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Correo))
            {
                return ResultadoOperacion<string>.Fallo("El correo es requerido", 400);
            }
            if (string.IsNullOrEmpty(dto.Contraseña))
            {
                return ResultadoOperacion<string>.Fallo("La contraseña es requerida", 400);
            }
            if (string.IsNullOrEmpty(dto.Rol))
            {
                return ResultadoOperacion<string>.Fallo("El rol es requerido", 400);
            }
            if (string.IsNullOrEmpty(dto.Rut))
            {
                return ResultadoOperacion<string>.Fallo("El RUT es requerido", 400);
            }
            if (string.IsNullOrEmpty(dto.Nombre))
            {
                return ResultadoOperacion<string>.Fallo("El nombre es requerido", 400);
            }

            // Validar formato y dígito verificador del RUT
            var validacionRut = ValidadorRut.ValidarRut(dto.Rut);
            if (validacionRut == ResultadoRut.FormatoInvalido)
            {
                return ResultadoOperacion<string>.Fallo("El formato del RUT es inválido. Debe ser: XX.XXX.XXX-X", 400);
            }
            if (validacionRut == ResultadoRut.DigitoVerificadorIncorrecto)
            {
                return ResultadoOperacion<string>.Fallo("El dígito verificador del RUT es incorrecto", 400);
            }

            var usuarioExistenete = await _userManager.FindByEmailAsync(dto.Correo!);
            if (usuarioExistenete != null) // Verificacion de existencia de correo
            {
                return ResultadoOperacion<string>.Fallo("Correo ya ingresado", 400);
            }

            bool rutExiste = await (_userManager.Users.AnyAsync(Usuario => Usuario.Rut == dto.Rut));
            if (rutExiste) // Verificacion de rut existente, asi no dejaría crear un usuario
            {
                return ResultadoOperacion<string>.Fallo("RUT ya ingresado al sistema", 400);
            }
            bool rolExiste = await (_roleManager.RoleExistsAsync(dto.Rol!));
            if (!rolExiste) // Verifica si el rol ingresado existe; si no, se cancela la operación.
            {
                return ResultadoOperacion<string>.Fallo("El rol no existe", 400);
            }
            Usuario usuario = new Usuario // Creacion de una instancia del usuario a partir de los datos recibidos en el DTO.
            {
                Nombre = dto.Nombre!,
                Rut = dto.Rut!,
                Email = dto.Correo!,
                UserName = dto.Correo!,
                Rol = dto.Rol!
            };
            IdentityResult resultado = await _userManager.CreateAsync(usuario, dto.Contraseña!); // Crear Usuario y contraseña
            if (!resultado.Succeeded) // Verifica si la creación del usuario fue exitosa; si no, retorna un error.
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                return ResultadoOperacion<string>.Fallo($"Error al crear el usuario: {errores}", 400);
            }
            await _userManager.AddToRoleAsync(usuario, dto.Rol); // Asignación de rol

            return ResultadoOperacion<string>.Exito("Usuario creado con éxito"); // Retorno el exito de la operacion crear usuario
        }
        public async Task<ResultadoOperacion<ResultadoLoginDTO>> LoginUsuarioAsync(UsuarioLoginDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Correo))
            {
                return ResultadoOperacion<ResultadoLoginDTO>.Fallo("El correo es requerido", 400);
            }
            if (string.IsNullOrEmpty(dto.Contraseña))
            {
                return ResultadoOperacion<ResultadoLoginDTO>.Fallo("La contraseña es requerida", 400);
            }

            var usuario = await _userManager.FindByEmailAsync(dto.Correo!);
            if (usuario == null)
            {
                return ResultadoOperacion<ResultadoLoginDTO>.Fallo("Credenciales inválidas", 401);
            }
            var esValida = await _userManager.CheckPasswordAsync(usuario, dto.Contraseña!);
            if (!esValida)
            {
                return ResultadoOperacion<ResultadoLoginDTO>.Fallo("Contraseña incorrecta", 401);
            }

            var token = _tokenService.GenerarToken(usuario);
            var loginDto = new ResultadoLoginDTO { Token = token };

            return ResultadoOperacion<ResultadoLoginDTO>.Exito(loginDto, "Autenticación correcta");
        }
        public async Task<ResultadoOperacion<string>> RecuperarPasswordAsync(RecuperarPasswordDTO dto)
        {
            _logger.LogInformation($"🔐 Iniciando recuperación de contraseña para: {dto.Correo}");

            var usuario = await _userManager.FindByEmailAsync(dto.Correo);

            if (usuario == null)
            {
                _logger.LogWarning($"❌ Usuario no encontrado: {dto.Correo}");
                return ResultadoOperacion<string>.Fallo("No se encontró ninguna cuenta asociada a ese correo", 404);
            }

            _logger.LogInformation($"✅ Usuario encontrado: {usuario.Nombre} ({usuario.Email})");

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            _logger.LogInformation($"🔑 Token generado para {usuario.Email}");

            // URL de producción para recuperación de contraseña
            var link = $"http://135.148.148.88:3000/reset-password?token={Uri.EscapeDataString(token)}&email={usuario.Email}";
            _logger.LogInformation($"🔗 Link generado: {link.Substring(0, Math.Min(100, link.Length))}...");

            try
            {
                _logger.LogInformation($"📧 Intentando enviar email a {usuario.Email} via SendGrid...");

                // Enviar email usando SendGrid con template profesional
                await _emailService.EnviarRecuperacionPasswordAsync(usuario.Email!, usuario.Nombre, link);

                _logger.LogInformation($"✅ Email enviado exitosamente a {usuario.Email}");
                var mensajeExito = "Se ha enviado un correo con instrucciones para restablecer tu contraseña";
                return ResultadoOperacion<string>.Exito(mensajeExito, mensajeExito);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error al enviar email a {usuario.Email}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return ResultadoOperacion<string>.Fallo($"Error al enviar el correo de recuperación: {ex.Message}", 500);
            }
        }
        public async Task<ResultadoOperacion<string>> RestablecerPasswordAsync(RestablecerPasswordDTO dto)
        {
            var usuario = await _userManager.FindByEmailAsync(dto.Correo);

            if (usuario == null)
            {
                return ResultadoOperacion<string>.Fallo("No hay cuenta asociada al correo", 404);
            }

            var resultado = await _userManager.ResetPasswordAsync(usuario, dto.Token, dto.NuevaContraseña);

            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));

                return ResultadoOperacion<string>.Fallo($"No se pudo actualizar la contraseña: {errores}", 400);
            }
            return ResultadoOperacion<string>.Exito("Contraseña actualizada correctamente");
        }
        public Task<ResultadoOperacion<UsuarioProfileDTO>> ObtenerPerfilAsync()
        {
            var contexto = _httpContextAccessor.HttpContext; // Accediendo al contexto, que ayuda a acceder a los claims del JWT Token

            if (contexto == null)
            {
                return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Fallo(mensaje: "No hay contexto", 400));
            }

            var usuario = contexto.User; // Accediendo al usuario que se define en el claim del JWT Token

            if (usuario == null)
            {
                return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Fallo(mensaje: "Usuario no encontrado", 400));
            }

            var nombre = usuario.FindFirst("nombre")?.Value; // Accediendo al nombre que se define en el claim del JWT Token

            if (nombre == null)
            {
                return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Fallo(mensaje: "Nombre nulo desde el JWT", 400));
            }

            var rol = usuario.FindFirst(ClaimTypes.Role)?.Value; // Accediendo al rol que se define en el claim del JWT Token

            if (rol == null)
            {
                return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Fallo(mensaje: "Rol nulo desde el JWT", 400));
            }

            var correo = usuario.FindFirst(ClaimTypes.Email); // Accediendo al correo que se define en el claim del JWT Token
            if (correo == null)
            {
                return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Fallo(mensaje: "Correo nulo desde el JWT", 400));
            }

            var dato = new UsuarioProfileDTO // Creando objeto DTO, con datos que vienen de los claims del JWT Token.
            {
                Nombre = nombre,
                Correo = correo.Value, // Valor del correo, que viene en el claim ej: example@correo.com
                Rol = rol
            };

            return Task.FromResult(ResultadoOperacion<UsuarioProfileDTO>.Exito(dato));
        }
        public async Task<ResultadoOperacion<List<UsuarioDTO>>> ListarUsuariosAsync(PaginacionDTO dto)
        {
            // 1. Validar parámetros de paginación
            if (dto.PaginaActual < 1 || dto.CantidadPorPagina < 1)
            {
                return ResultadoOperacion<List<UsuarioDTO>>.Fallo("Parámetros de paginación inválidos", 400);
            }
            // 2. Calcular total de usuarios para metadata (opcional)
            var totalUsuarios = await _userManager.Users.CountAsync();
            dto.TotalResultados = totalUsuarios;
            dto.TotalPaginas = (int)Math.Ceiling((double)totalUsuarios / dto.CantidadPorPagina);

            // 3. Calcular elementos a saltar
            var skip = (dto.PaginaActual - 1) * dto.CantidadPorPagina;

            // 4. Obtener usuarios paginados
            var usuariosEntidades = await _userManager.Users
                .OrderBy(u => u.Nombre)
                .Skip(skip)
                .Take(dto.CantidadPorPagina)
                .ToListAsync();

            // 5. Mapear a DTO
            var usuariosDto = usuariosEntidades
                .Select(u => new UsuarioDTO
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Email!,
                    Rut = u.Rut,
                    Rol = u.Rol,
                    FechaCreacion = u.FechaCreacion
                })
                .ToList();

            // 6. Retornar resultado
            return ResultadoOperacion<List<UsuarioDTO>>.Exito(usuariosDto);
        }
        public async Task<ResultadoOperacion<UsuarioDTO>> ObtenerUsuarioPorIdAsync(string id)
        {
            // 1. Buscar el usuario por Id con perfil incluido
            var usuario = await _userManager.Users
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return ResultadoOperacion<UsuarioDTO>.Fallo("Usuario no encontrado", 404);
            }

            // 2. Obtener URL de la foto de perfil desde S3
            string? fotoPerfilUrl = null;

            if (usuario.Perfil != null && !string.IsNullOrEmpty(usuario.Perfil.AvatarFileId))
            {
                try
                {
                    if (_storageService is S3StorageService s3Service)
                    {
                        var downloadResponse = await s3Service.GeneratePresignedDownloadUrlAsync(
                            id,
                            new DTOs.GenerateDownloadUrlRequestDTO
                            {
                                FileId = usuario.Perfil.AvatarFileId,
                                Subfolder = "avatars"
                            }
                        );
                        fotoPerfilUrl = downloadResponse.PresignedUrl;
                    }
                    else
                    {
                        // Para LocalStorageService, usar la URL almacenada
                        fotoPerfilUrl = usuario.Perfil.AvatarUrl;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al generar URL de avatar para usuario {UserId}", id);
                    fotoPerfilUrl = null;
                }
            }

            // 3. Mapear a UsuarioDTO
            var usuarioDto = new UsuarioDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Email!,
                Rut = usuario.Rut,
                Rol = usuario.Rol,
                FechaCreacion = usuario.FechaCreacion,
                FotoPerfil = fotoPerfilUrl
            };

            // 4. Retornar resultado exitoso
            return ResultadoOperacion<UsuarioDTO>.Exito(usuarioDto);
        }
        public async Task<ResultadoOperacion<string>> EditarUsuarioAsync(string id, UsuarioEdicionDTO dto)
        {
            // 1. Buscar el usuario por Id
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);
            }

            // 2. Validar correo (si cambia)
            if (!string.IsNullOrWhiteSpace(dto.Correo) && dto.Correo != usuario.Email)
            {
                var emailNormalizado = dto.Correo.Trim().ToLower();
                var existeEmail = await _userManager.FindByEmailAsync(emailNormalizado);
                if (existeEmail != null)
                {
                    return ResultadoOperacion<string>.Fallo("El correo ya está en uso", 400);
                }
                usuario.Email = emailNormalizado;
                usuario.UserName = emailNormalizado;
            }

            // 3. Actualizar nombre y rol (si vienen)
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
            {
                usuario.Nombre = dto.Nombre;
            }
            if (!string.IsNullOrWhiteSpace(dto.Rol) && dto.Rol != usuario.Rol)
            {
                // Verificar existencia del rol
                if (!await _roleManager.RoleExistsAsync(dto.Rol))
                {
                    return ResultadoOperacion<string>.Fallo("El rol especificado no existe", 400);
                }
                // Remover rol anterior e insertar nuevo
                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                await _userManager.AddToRoleAsync(usuario, dto.Rol);
                usuario.Rol = dto.Rol;
            }

            // 4. Guardar cambios
            var resultado = await _userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                return ResultadoOperacion<string>.Fallo($"Error al actualizar usuario: {errores}", 400);
            }

            return ResultadoOperacion<string>.Exito("Usuario actualizado correctamente");
        }
        public async Task<ResultadoOperacion<string>> EliminarUsuarioAsync(string id)
        {
            // 1. Buscar el usuario por Id
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);
            }

            // 2. (Opcional) Remover roles asignados
            var roles = await _userManager.GetRolesAsync(usuario);
            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(usuario, roles);
            }

            // 3. Eliminar usuario
            var resultado = await _userManager.DeleteAsync(usuario);
            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                return ResultadoOperacion<string>.Fallo($"Error al eliminar usuario: {errores}", 400);
            }

            return ResultadoOperacion<string>.Exito("Usuario eliminado correctamente");
        }
        public async Task<ResultadoOperacion<string>> ActualizarDescripcionAsync(string userId, string? nuevaDescripcion)
        {
            // Buscar usuario por ID, incluyendo el perfil.

            var usuario = await _userManager.Users
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Id == userId);

            // Verifica si el usuario fue encontrado, si no es asi retorna fallo

            if (usuario == null)
            {
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);
            }

            // Maneja la logica de actualizacion, crea un nuevo perfil si no existe o actualiza el actual.

            if (usuario.Perfil == null)
            {
                // El usuario no tiene un perfil, por lo que se crea uno nuevo.

                usuario.Perfil = new UsuarioPerfil
                {
                    UsuarioId = usuario.Id,
                    Descripcion = nuevaDescripcion,
                };
            }
            else
            {   // El usuario ya tiene un perfil, solo se actualiza la descripcion.

                usuario.Perfil.Descripcion = nuevaDescripcion;
            }
            // Persiste los cambios del usuario y su perfil en la base de datos.

            await _userManager.UpdateAsync(usuario);

            // Retorna un resultado exitoso.

            return ResultadoOperacion<string>.Exito("Perfil actualizado correctamente");
        }
        public async Task<ResultadoOperacion<UsuarioPerfilDTO>> ObtenerPerfilCompletoAsync(string userId)
        {
            var usuario = await _userManager.Users
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
            {
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo("Usuario no encontrado", 404);
            }

            // Crear perfil si no existe
            if (usuario.Perfil == null)
            {
                usuario.Perfil = new UsuarioPerfil
                {
                    UsuarioId = usuario.Id
                };
                await _userManager.UpdateAsync(usuario);
            }

            var dto = new UsuarioPerfilDTO
            {
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Correo = usuario.Email ?? string.Empty,
                Rol = usuario.Rol,
                Descripcion = usuario.Perfil?.Descripcion,
                TieneAvatar = !string.IsNullOrEmpty(usuario.Perfil?.AvatarFileId)
            };

            return ResultadoOperacion<UsuarioPerfilDTO>.Exito(dto);
        }
        public async Task<ResultadoOperacion<UsuarioPerfilDTO>> ActualizarAvatarAsync(string userId, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo("Archivo vació", 400);
            }

            // Validar tipo
            var tiposPermitidos = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!tiposPermitidos.Contains(archivo.ContentType?.ToLowerInvariant()))
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo("Formato de imagen no permitido. Solo se permiten JPG y PNG", 415);

            // Validar tamaño (ej. máx 2 MB)
            if (archivo.Length > 2 * 1024 * 1024)
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo("El archivo excede el límite de 2MB", 400);

            var usuario = await _userManager.Users
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo("Usuario no encontrado", 404);

            try
            {
                // Eliminar avatar anterior de S3 si existe
                if (usuario.Perfil != null && !string.IsNullOrEmpty(usuario.Perfil.AvatarFileId))
                {
                    try
                    {
                        await _storageService.DeleteFileAsync(usuario.Perfil.AvatarFileId, "avatars");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo eliminar el avatar anterior del usuario {UserId}", userId);
                    }
                }

                // Subir nuevo avatar a S3
                var (fileId, fileUrl) = await _storageService.SaveFileAsync(userId, archivo, "avatars");

                // Actualizar perfil con referencia a S3
                if (usuario.Perfil == null)
                {
                    usuario.Perfil = new UsuarioPerfil
                    {
                        UsuarioId = userId,
                        AvatarFileId = fileId,
                        AvatarUrl = fileUrl,
                        AvatarBytes = null // Ya no guardar en BD
                    };
                }
                else
                {
                    usuario.Perfil.AvatarFileId = fileId;
                    usuario.Perfil.AvatarUrl = fileUrl;
                    usuario.Perfil.AvatarBytes = null; // Limpiar bytes antiguos
                }

                await _userManager.UpdateAsync(usuario);

                var dto = new UsuarioPerfilDTO
                {
                    UsuarioId = usuario.Id,
                    Nombre = usuario.Nombre,
                    Correo = usuario.Email!,
                    Rol = usuario.Rol,
                    Descripcion = usuario.Perfil?.Descripcion,
                    TieneAvatar = !string.IsNullOrEmpty(usuario.Perfil?.AvatarFileId),
                    AvatarUrl = fileUrl
                };

                return ResultadoOperacion<UsuarioPerfilDTO>.Exito(dto, "Foto de perfil actualizada correctamente en S3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir avatar a S3 para usuario {UserId}", userId);
                return ResultadoOperacion<UsuarioPerfilDTO>.Fallo($"Error al subir la imagen: {ex.Message}", 500);
            }
        }
        public async Task<ResultadoOperacion<string>> EliminarAvatarAsync(string userId)
        {
            var usuario = await _userManager.Users
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);

            if (usuario.Perfil == null || (usuario.Perfil.AvatarBytes == null && string.IsNullOrEmpty(usuario.Perfil.AvatarFileId)))
                return ResultadoOperacion<string>.Fallo("El usuario no tiene avatar", 400);

            try
            {
                // Eliminar archivo de S3 si existe
                if (!string.IsNullOrEmpty(usuario.Perfil.AvatarFileId))
                {
                    await _storageService.DeleteFileAsync(usuario.Perfil.AvatarFileId, "avatars");
                }

                // Limpiar referencias del perfil
                usuario.Perfil.AvatarFileId = null;
                usuario.Perfil.AvatarUrl = null;
                usuario.Perfil.AvatarBytes = null;

                await _userManager.UpdateAsync(usuario);

                return ResultadoOperacion<string>.Exito("Avatar eliminado correctamente de S3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar avatar de S3 para usuario {UserId}", userId);
                return ResultadoOperacion<string>.Fallo($"Error al eliminar la imagen: {ex.Message}", 500);
            }
        }

        public async Task<ResultadoOperacion<string>> ObtenerUrlAvatarAsync(string userId)
        {
            var usuario = await _userManager.Users
                .Include(u => u.Perfil)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);

            if (usuario.Perfil == null || string.IsNullOrEmpty(usuario.Perfil.AvatarFileId))
            {
                // Verificar si tiene avatar antiguo en bytes
                if (usuario.Perfil?.AvatarBytes != null)
                {
                    return ResultadoOperacion<string>.Fallo("Avatar en formato antiguo, debe actualizarse", 404);
                }
                return ResultadoOperacion<string>.Fallo("El usuario no tiene avatar", 404);
            }

            try
            {
                // Generar URL prefirmada de S3
                if (_storageService is S3StorageService s3Service)
                {
                    var downloadResponse = await s3Service.GeneratePresignedDownloadUrlAsync(
                        userId,
                        new DTOs.GenerateDownloadUrlRequestDTO
                        {
                            FileId = usuario.Perfil.AvatarFileId,
                            Subfolder = "avatars"
                        }
                    );
                    return ResultadoOperacion<string>.Exito(downloadResponse.PresignedUrl);
                }
                else
                {
                    // Para LocalStorageService
                    return ResultadoOperacion<string>.Exito(usuario.Perfil.AvatarUrl ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL de avatar para usuario {UserId}", userId);
                return ResultadoOperacion<string>.Fallo("Error al obtener el avatar", 500);
            }
        }

        public async Task<ResultadoOperacion<string>> ActualizarNombreAsync(string userId, string nuevoNombre)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            if (usuario == null)
            {
                return ResultadoOperacion<string>.Fallo("Usuario no encontrado", 404);
            }

            if (string.IsNullOrWhiteSpace(nuevoNombre) || nuevoNombre.Length > 120)
            {
                return ResultadoOperacion<string>.Fallo("El nombre es inválido (requerido y máx. 120 caracteres)", 400);
            }

            usuario.Nombre = nuevoNombre.Trim();

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                return ResultadoOperacion<string>.Fallo($"Error al actualizar nombre: {errores}", 400);
            }

            return ResultadoOperacion<string>.Exito("Nombre actualizado correctamente");
        }

        // Método temporal para asignar RUTs de prueba a usuarios existentes
        public async Task<ResultadoOperacion<string>> AsignarRutsDePruebaAsync()
        {
            try
            {
                var usuarios = await _userManager.Users.Where(u => string.IsNullOrEmpty(u.Rut)).ToListAsync();
                
                string[] rutsDePrueba = {
                    "12.345.678-9", "13.456.789-0", "14.567.890-1", "15.678.901-2", 
                    "16.789.012-3", "17.890.123-4", "18.901.234-5", "19.012.345-6",
                    "20.123.456-7", "21.234.567-8", "22.345.678-9", "23.456.789-0"
                };

                int rutIndex = 0;
                foreach (var usuario in usuarios)
                {
                    if (rutIndex < rutsDePrueba.Length)
                    {
                        usuario.Rut = rutsDePrueba[rutIndex];
                        await _userManager.UpdateAsync(usuario);
                        rutIndex++;
                    }
                }

                return ResultadoOperacion<string>.Exito($"Se asignaron RUTs de prueba a {Math.Min(usuarios.Count, rutsDePrueba.Length)} usuarios");
            }
            catch (Exception ex)
            {
                return ResultadoOperacion<string>.Fallo($"Error al asignar RUTs: {ex.Message}", 500);
            }
        }

        public async Task<Usuario?> ObtenerUsuarioConPerfilAsync(string userId)
        {
            return await _userManager.Users
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
