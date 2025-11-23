using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LMSBackend.API.Models;

namespace LMSBackend.API.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GenerarToken(Usuario usuario)
        {

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
                new Claim("nombre", usuario.Nombre ?? string.Empty),
                new Claim(ClaimTypes.Role, usuario.Rol ?? string.Empty)
            };

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key missing in configuration");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var isThreeDLabIntegrationUser = string.Equals(usuario.Email, "3dlab@laboratorio.edu", StringComparison.OrdinalIgnoreCase)
                || string.Equals(usuario.Rut, "99999999-9", StringComparison.OrdinalIgnoreCase)
                || string.Equals(usuario.Nombre, "Integración 3DLAB", StringComparison.OrdinalIgnoreCase);

            // Usuario 3DLAB tiene token de larga duración (10 años), otros usuarios 25 minutos
            DateTime? expiresAt = isThreeDLabIntegrationUser ? DateTime.UtcNow.AddYears(10) : DateTime.UtcNow.AddMinutes(25);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
