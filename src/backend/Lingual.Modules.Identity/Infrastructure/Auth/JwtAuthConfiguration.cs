using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Lingual.Modules.Identity.Infrastructure.Auth;

public static class JwtAuthConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:Key"]
            ?? configuration["JWT_SECRET"]
            ?? "";
        var issuer = configuration["Jwt:Issuer"] ?? "Lingual.Api";
        var audience = configuration["Jwt:Audience"] ?? "Lingual.Clients";

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            // Support SignalR WebSocket query string token & Token revocation check
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/duelHub")))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value
                           ?? context.Principal?.FindFirst("jti")?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        var memoryCache = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                        if (memoryCache != null && memoryCache.TryGetValue($"revoked_token:{jti}", out _))
                        {
                            context.Fail("Token đã bị thu hồi (người dùng đã đăng xuất).");
                        }
                    }
                    return Task.CompletedTask;
                }

            };
        });

        services.AddAuthorization();

        return services;
    }
}
