using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public class TokenService : ITokenService
{
    private const int DefaultAccessTokenExpirationMinutes = 60;
    private const int DefaultRefreshTokenExpirationDays = 30;
    private readonly ILogger<TokenService> _logger;
    private readonly IConfiguration _configuration;

    public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public AccessTokenResult CreateAccessToken(User user)
    {
        var expirationMinutes = GetOptionalIntJwtSetting("AccessTokenExpirationMinutes", DefaultAccessTokenExpirationMinutes);
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var token = CreateJwtToken(
            CreateClaims(user),
            CreateSigningCredentials(),
            expiration
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenValue = tokenHandler.WriteToken(token);

        _logger.LogInformation("JWT Token created for user {UserId}", user.Id);

        return new AccessTokenResult
        {
            Token = tokenValue,
            ExpiresIn = (int)Math.Max(0, (expiration - DateTime.UtcNow).TotalSeconds),
            ExpiresAt = expiration
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncoder.Encode(randomBytes);
    }

    public DateTime GetRefreshTokenExpiration()
    {
        var refreshExpirationDays = GetOptionalIntJwtSetting("RefreshTokenExpirationDays", DefaultRefreshTokenExpirationDays);
        return DateTime.UtcNow.AddDays(refreshExpirationDays);
    }

    private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials, DateTime expiration)
    {
        return new JwtSecurityToken(
            issuer: GetRequiredJwtSetting("ValidIssuer"),
            audience: GetRequiredJwtSetting("ValidAudience"),
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );
    }

    private List<Claim> CreateClaims(User user)
    {
        var sub = user.Id.ToString();
        var displayName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : $"User_{user.MezonUserId}";
        var role = !string.IsNullOrWhiteSpace(user.Role) ? user.Role : "learner";

        return new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sub),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, sub),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, role),
            new("role", role),
            new("mezon_user_id", user.MezonUserId ?? string.Empty)
        };

    }

    private SigningCredentials CreateSigningCredentials()
    {
        var symmetricSecurityKey = GetRequiredJwtSetting("SymmetricSecurityKey");

        return new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(symmetricSecurityKey)
            ),
            SecurityAlgorithms.HmacSha256
        );
    }

    private string GetRequiredJwtSetting(string key)
    {
        var value = _configuration[$"JwtTokenSettings:{key}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            // Fallback to "Jwt:..." naming convention in appsettings.json
            value = key switch
            {
                "ValidIssuer" => _configuration["Jwt:Issuer"],
                "ValidAudience" => _configuration["Jwt:Audience"],
                "SymmetricSecurityKey" => _configuration["Jwt:Key"] ?? _configuration["JWT_SECRET"],
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            // Safe dev defaults
            value = key switch
            {
                "ValidIssuer" => "Lingual.Api",
                "ValidAudience" => "Lingual.Clients",
                "SymmetricSecurityKey" => "LingualSuperSecretKeyForDevelopment2026McsStudioAuth!",
                _ => null
            };
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing JwtTokenSettings:{key} or Jwt configuration.");
        }

        return value;
    }

    private int GetOptionalIntJwtSetting(string key, int defaultValue)
    {
        var value = _configuration[$"JwtTokenSettings:{key}"];

        if (string.IsNullOrWhiteSpace(value) && key == "AccessTokenExpirationMinutes")
        {
            var expiryDays = _configuration["Jwt:ExpiryDays"];
            if (int.TryParse(expiryDays, out var parsedDays) && parsedDays > 0)
            {
                return parsedDays * 24 * 60;
            }
        }

        return int.TryParse(value, out var parsedValue) && parsedValue > 0
            ? parsedValue
            : defaultValue;
    }
}

