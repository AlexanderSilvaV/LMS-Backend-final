using LMSBackend.API.Configuration;
using LMSBackend.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace LMSBackend.API.Helpers
{
    public static class ThreeDLabInitializer
    {
        public static async Task EnsureIntegrationUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ThreeDLabInitializer");

            var options = scope.ServiceProvider.GetRequiredService<IOptions<ThreeDLabOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ServiceUserEmail))
            {
                logger.LogWarning("3DLAB: ServiceUserEmail no configurado. Se omite la creación del usuario de integración.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.ServiceUserPassword))
            {
                logger.LogWarning("3DLAB: ServiceUserPassword no configurado. Se omite la creación del usuario de integración.");
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roleName = string.IsNullOrWhiteSpace(options.ServiceUserRole) ? "Docente" : options.ServiceUserRole.Trim();
            var desiredName = string.IsNullOrWhiteSpace(options.ServiceUserName) ? "Integración 3DLAB" : options.ServiceUserName.Trim();
            var desiredRut = string.IsNullOrWhiteSpace(options.ServiceUserRut) ? "99999999-9" : options.ServiceUserRut.Trim();

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                    logger.LogError("3DLAB: No se pudo crear el rol {Role}. Errores: {Errors}", roleName, errors);
                    return;
                }
            }

            var email = options.ServiceUserEmail.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new Usuario
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Nombre = desiredName,
                    Rut = desiredRut,
                    Rol = roleName,
                };

                var createResult = await userManager.CreateAsync(user, options.ServiceUserPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                    logger.LogError("3DLAB: No se pudo crear el usuario de integración. Errores: {Errors}", errors);
                    return;
                }

                logger.LogInformation("3DLAB: Usuario de integración creado correctamente ({Email}).", email);
            }
            else
            {
                var requiresUpdate = false;

                if (!string.Equals(user.Nombre, desiredName, StringComparison.Ordinal))
                {
                    user.Nombre = desiredName;
                    requiresUpdate = true;
                }

                if (!string.Equals(user.Rut, desiredRut, StringComparison.Ordinal))
                {
                    user.Rut = desiredRut;
                    requiresUpdate = true;
                }

                if (!string.Equals(user.Rol, roleName, StringComparison.Ordinal))
                {
                    user.Rol = roleName;
                    requiresUpdate = true;
                }

                if (requiresUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(", ", updateResult.Errors.Select(error => error.Description));
                        logger.LogWarning("3DLAB: No se pudo actualizar el usuario de integración. Errores: {Errors}", errors);
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
                if (!addToRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addToRoleResult.Errors.Select(error => error.Description));
                    logger.LogWarning("3DLAB: No se pudo asignar el rol {Role} al usuario {Email}. Errores: {Errors}", roleName, email, errors);
                }
            }
        }
    }
}
