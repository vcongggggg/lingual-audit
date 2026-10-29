using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;

public class LoginWithMezonEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 1. Authorize URL endpoints
        app.MapGet("/api/v1/auth/mezon/authorize", async (IMezonAuthService mezonAuthService) =>
        {
            var result = await mezonAuthService.GetAuthorizeUrlAsync();
            return Results.Json(result.Payload, statusCode: result.StatusCode);
        })
        .WithName("GetMezonAuthorizeUrl")
        .WithTags("Auth")
        .WithSummary("Lấy URL ủy quyền đăng nhập Mezon OAuth 2.0")
        .WithDescription("Sinh URL chuyển hướng tới trang ủy quyền của Mezon kèm mã state ngẫu nhiên chống CSRF.")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();

        app.MapGet("/api/v1/auth/mezon/authorize-url", async (IMezonAuthService mezonAuthService) =>
        {
            var result = await mezonAuthService.GetAuthorizeUrlAsync();
            return Results.Json(result.Payload, statusCode: result.StatusCode);
        })
        .WithName("GetMezonAuthorizeUrlAlias")
        .WithTags("Auth")
        .WithSummary("Alias lấy URL ủy quyền Mezon OAuth 2.0")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 2. Callback & Token exchange endpoints
        app.MapPost("/api/v1/auth/mezon", async (MezonAuthRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginWithMezonCommand(request), ct);
            return Results.Json(result.Payload, statusCode: result.StatusCode);
        })
        .WithName("LoginWithMezon")
        .WithTags("Auth")
        .WithSummary("Đăng nhập hoặc đăng ký tự động qua Mezon OAuth2 Callback")
        .WithDescription("Tiếp nhận authorization code và state từ Mezon OAuth callback, tự động đổi token, tạo tài khoản và trả về JWT Bearer Token.")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        app.MapPost("/api/v1/auth/mezon-sso", async (MezonAuthRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginWithMezonCommand(request), ct);
            return Results.Json(result.Payload, statusCode: result.StatusCode);
        })
        .WithName("LoginWithMezonSso")
        .WithTags("Auth")
        .WithSummary("Alias cho endpoint đăng nhập Mezon SSO")
        .Produces<AuthResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // 3. Refresh Token
        app.MapPost("/api/v1/auth/refresh", async (RefreshTokenRequest request, ITokenService tokenService, IMemoryCache memoryCache, IdentityDbContext dbContext, CancellationToken ct) =>
        {
            var refreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
            var cacheKey = $"refresh_token:{refreshTokenHash}";

            if (!memoryCache.TryGetValue(cacheKey, out string? userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Results.BadRequest(new { Message = "Refresh token không hợp lệ hoặc đã hết hạn." });
            }

            var user = await dbContext.Users.FindAsync([userId], ct);
            if (user == null)
            {
                return Results.BadRequest(new { Message = "Người dùng không tồn tại." });
            }

            memoryCache.Remove(cacheKey);

            var newAccessToken = tokenService.CreateAccessToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            var newRefreshHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(newRefreshToken)));
            var expiration = tokenService.GetRefreshTokenExpiration();

            memoryCache.Set($"refresh_token:{newRefreshHash}", user.Id.ToString(), expiration - DateTime.UtcNow);

            return Results.Ok(new
            {
                Token = newAccessToken.Token,
                AccessToken = newAccessToken.Token,
                RefreshToken = newRefreshToken,
                ExpiresIn = newAccessToken.ExpiresIn
            });
        })
        .WithName("RefreshToken")
        .WithTags("Auth")
        .WithSummary("Làm mới access token bằng refresh token")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // 4. Logout & Token Revocation
        app.MapPost("/api/v1/auth/logout", (LogoutRequest? request, HttpContext httpContext, IMemoryCache memoryCache) =>
        {
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);
                        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti" || c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                        if (!string.IsNullOrEmpty(jti))
                        {
                            var ttl = jwtToken.ValidTo > DateTime.UtcNow ? jwtToken.ValidTo - DateTime.UtcNow : TimeSpan.FromMinutes(5);
                            memoryCache.Set($"revoked_token:{jti}", true, ttl);
                        }
                    }
                }
                catch
                {
                    // Ignore parsing error on logout
                }
            }

            if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                var refreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
                memoryCache.Remove($"refresh_token:{refreshTokenHash}");
            }

            return Results.Ok(new { Message = "Đăng xuất và thu hồi phiên làm việc thành công." });
        })
        .WithName("Logout")
        .WithTags("Auth")
        .WithSummary("Đăng xuất và thu hồi JWT Bearer token / refresh token")
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 5. Me - Check current user & token validity
        app.MapGet("/api/v1/auth/me", async (HttpContext httpContext, IdentityDbContext dbContext) =>
        {
            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var mezonUserId = httpContext.User.FindFirst("mezon_user_id")?.Value;

            string? avatarUrl = null;
            if (Guid.TryParse(userIdStr, out var uid))
            {
                var user = await dbContext.Users.FindAsync([uid]);
                avatarUrl = user?.AvatarUrl;
            }

            return Results.Ok(new
            {
                userId = userIdStr,
                name,
                role,
                mezonUserId,
                avatarUrl
            });
        })

        .WithName("GetCurrentUser")
        .WithTags("Auth")
        .WithSummary("Lấy thông tin người dùng từ Bearer JWT token")
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
