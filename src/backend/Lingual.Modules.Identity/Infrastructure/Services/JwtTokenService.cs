using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lingual.Modules.Identity.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, int ExpiresIn) GenerateToken(User user)
    {
        var secretKey = configuration["Jwt:Key"] 
            ?? configuration["JWT_SECRET"] 
            ?? "LingualSuperSecretKeyForDevelopment2026McsStudioAuth!";
        var issuer = configuration["Jwt:Issuer"] ?? "Lingual.Api";
        var audience = configuration["Jwt:Audience"] ?? "Lingual.Clients";
        
        var expiryDays = 7;
        if (int.TryParse(configuration["Jwt:ExpiryDays"], out var days) && days > 0)
        {
            expiryDays = days;
        }

        var expires = DateTime.UtcNow.AddDays(expiryDays);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new("mezonUserId", user.MezonUserId),
            new("name", user.DisplayName),
            new("role", user.Role),
            new("status", user.Status),
            new("jti", Guid.NewGuid().ToString())
        };

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(jwtSecurityToken);

        var expiresInSeconds = (int)(expires - DateTime.UtcNow).TotalSeconds;

        return (tokenString, expiresInSeconds);
    }
}
