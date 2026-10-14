using Lingual.Infrastructure.Endpoint;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Services;
using Lingual.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lingual.Modules.Identity.Features.Auth.LoginWithMezon;

public class LoginWithMezonEndpoint : IEndpointBase
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 1. Authorize URL endpoints
        app.MapGet("/api/v1/auth/mezon/authorize", (IMezonAuthService mezonAuthService, string? redirectUri) =>
        {
            var result = mezonAuthService.GetAuthorizeUrl(redirectUri);
            return Results.Ok(ApiResponse<MezonAuthorizeUrlDto>.Ok(result, "Tạo URL ủy quyền Mezon thành công."));
        })
        .WithName("GetMezonAuthorizeUrl")
        .WithTags("Auth")
        .WithSummary("Lấy URL ủy quyền đăng nhập Mezon OAuth 2.0")
        .WithDescription("Sinh URL chuyển hướng tới trang ủy quyền của Mezon kèm mã state ngẫu nhiên chống CSRF.")
        .Produces<ApiResponse<MezonAuthorizeUrlDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        app.MapGet("/api/v1/auth/mezon/authorize-url", (IMezonAuthService mezonAuthService, string? redirectUri) =>
        {
            var result = mezonAuthService.GetAuthorizeUrl(redirectUri);
            return Results.Ok(ApiResponse<MezonAuthorizeUrlDto>.Ok(result, "Tạo URL ủy quyền Mezon thành công."));
        })
        .WithName("GetMezonAuthorizeUrlAlias")
        .WithTags("Auth")
        .WithSummary("Alias lấy URL ủy quyền Mezon OAuth 2.0")
        .Produces<ApiResponse<MezonAuthorizeUrlDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // 2. Callback & Token exchange endpoints
        app.MapPost("/api/v1/auth/mezon", async (LoginWithMezonRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginWithMezonCommand(request), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("LoginWithMezon")
        .WithTags("Auth")
        .WithSummary("Đăng nhập hoặc đăng ký tự động qua Mezon SSO Handshake / OAuth2 Callback")
        .WithDescription("Tiếp nhận authorization code và state từ Mezon OAuth callback, tự động đổi token, tạo tài khoản và trả về JWT Bearer Token.")
        .Produces<ApiResponse<AuthResultDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<AuthResultDto>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        app.MapPost("/api/v1/auth/mezon-sso", async (LoginWithMezonRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginWithMezonCommand(request), ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("LoginWithMezonSso")
        .WithTags("Auth")
        .WithSummary("Alias cho endpoint đăng nhập Mezon SSO")
        .Produces<ApiResponse<AuthResultDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<AuthResultDto>>(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // 3. Logout & Token Revocation
        app.MapPost("/api/v1/auth/logout", async (HttpContext httpContext, ITokenRevocationService revocationService, CancellationToken ct) =>
        {
            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                await revocationService.RevokeTokenAsync(token, ct);
            }

            return Results.Ok(ApiResponse<object?>.Ok(null, "Đăng xuất và thu hồi token thành công."));
        })
        .WithName("Logout")
        .WithTags("Auth")
        .WithSummary("Đăng xuất và thu hồi JWT Bearer token")
        .WithDescription("Vô hiệu hóa phiên làm việc của JWT token hiện tại trên toàn hệ thống.")
        .Produces<ApiResponse<object?>>(StatusCodes.Status200OK)
        .RequireAuthorization();

        // 4. Me - Check current user & token validity
        app.MapGet("/api/v1/auth/me", (HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var name = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var mezonUserId = httpContext.User.FindFirst("mezon_user_id")?.Value;

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                userId,
                name,
                role,
                mezonUserId
            }, "Xác thực token thành công."));
        })
        .WithName("GetCurrentUser")
        .WithTags("Auth")
        .WithSummary("Lấy thông tin người dùng từ Bearer JWT token")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
