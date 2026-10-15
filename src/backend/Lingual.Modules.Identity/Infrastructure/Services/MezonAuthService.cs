using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Domain;
using Lingual.Modules.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public class MezonAuthService : IMezonAuthService
{
    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<MezonAuthService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(5);
    private const string OAuthStateCacheKeyPrefix = "mezon_oauth_state:";

    public MezonAuthService(
        IdentityDbContext dbContext,
        ITokenService tokenService,
        ILogger<MezonAuthService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<MezonCallbackResult> GetAuthorizeUrlAsync()
    {
        var clientId = _configuration["MezonOAuth2:ClientId"];
        var authorizeUrl = _configuration["MezonOAuth2:AuthorizeUrl"] ?? "https://oauth2.mezon.ai/oauth2/auth";
        var redirectUri = _configuration["MezonOAuth2:RedirectUri"] ?? "http://localhost:3000/oauth/mezon/callback";
        var scope = _configuration["MezonOAuth2:Scope"] ?? "openid offline";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
        {
            _logger.LogError("Mezon OAuth is not configured correctly. Missing ClientId/RedirectUri.");
            return Task.FromResult(MezonCallbackResult.Failure(
                StatusCodes.Status500InternalServerError,
                new { Message = "Cấu hình OAuth của server chưa đầy đủ." }));
        }

        var state = GenerateMezonState();
        _memoryCache.Set(GetOAuthStateCacheKey(state), true, OAuthStateTtl);

        var authUrl =
            $"{authorizeUrl}?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Task.FromResult(MezonCallbackResult.Success(new 
        { 
            AuthorizeUrl = authUrl,
            State = state
        }));
    }

    public async Task<MezonCallbackResult> HandleCallbackAsync(MezonAuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "Authorization code is missing.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, new { Message = "Authorization code is missing." });
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "OAuth state is missing.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, new { Message = "OAuth state is missing." });
        }

        var stateCacheKey = GetOAuthStateCacheKey(request.State);
        if (!_memoryCache.TryGetValue(stateCacheKey, out _))
        {
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "Invalid or expired OAuth state.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status400BadRequest, new { Message = "Invalid or expired OAuth state." });
        }
        _memoryCache.Remove(stateCacheKey);

        var clientId = _configuration["MezonOAuth2:ClientId"];
        var clientSecret = _configuration["MezonOAuth2:ClientSecret"];
        var tokenUrl = _configuration["MezonOAuth2:TokenUrl"] ?? "https://oauth2.mezon.ai/oauth2/token";
        var userInfoUrl = _configuration["MezonOAuth2:UserInfoUrl"] ?? "https://oauth2.mezon.ai/userinfo";
        var configuredRedirectUri = !string.IsNullOrWhiteSpace(request.RedirectUri)
            ? request.RedirectUri
            : (_configuration["MezonOAuth2:RedirectUri"] ?? "http://localhost:3000/oauth/mezon/callback");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(configuredRedirectUri))
        {
            _logger.LogError("Mezon OAuth is not configured correctly. Missing ClientId/ClientSecret/RedirectUri.");
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "OAuth configuration is incomplete.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status500InternalServerError, new { Message = "Cấu hình OAuth của server chưa đầy đủ." });
        }

        var httpClient = _httpClientFactory.CreateClient();

        // 1. Primary: Standard OAuth2 client_secret_basic (RFC 6749 Section 2.3.1 requires URL-encoding credentials before Base64)
        var basicTokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        var encodedClientId = Uri.EscapeDataString(clientId);
        var encodedClientSecret = Uri.EscapeDataString(clientSecret);
        var authBytes = Encoding.UTF8.GetBytes($"{encodedClientId}:{encodedClientSecret}");
        basicTokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        basicTokenRequest.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", request.Code),
            new KeyValuePair<string, string>("redirect_uri", configuredRedirectUri)
        });

        var tokenResponse = await httpClient.SendAsync(basicTokenRequest);

        // 2. Fallback: client_secret_post if Basic Auth is rejected
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var postTokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", configuredRedirectUri)
                })
            };

            var postResponse = await httpClient.SendAsync(postTokenRequest);
            if (postResponse.IsSuccessStatusCode)
            {
                tokenResponse = postResponse;
            }
        }


        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("Mezon token exchange failed. Status: {StatusCode}, Body: {Body}", tokenResponse.StatusCode, errorContent);
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: $"Token exchange failed with status {(int)tokenResponse.StatusCode}.",
                status: "failed");
            return MezonCallbackResult.Failure((int)tokenResponse.StatusCode, new { Message = "Lỗi khi đổi token từ Mezon.", Details = errorContent });
        }

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (tokenData.ValueKind != JsonValueKind.Object ||
            !tokenData.TryGetProperty("access_token", out var accessTokenNode) ||
            string.IsNullOrWhiteSpace(accessTokenNode.GetString()))
        {
            var tokenRaw = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("Mezon token response does not contain access_token. Body: {Body}", tokenRaw);
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "Token response does not contain access_token.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi token từ Mezon không hợp lệ.", Details = tokenRaw });
        }

        var mezonAccessToken = accessTokenNode.GetString()!;

        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mezonAccessToken);

        var userInfoResponse = await httpClient.SendAsync(userInfoRequest);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            var userInfoError = await userInfoResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("Mezon userinfo failed. Status: {StatusCode}, Body: {Body}", userInfoResponse.StatusCode, userInfoError);
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: $"Userinfo request failed with status {(int)userInfoResponse.StatusCode}.",
                status: "failed");
            return MezonCallbackResult.Failure((int)userInfoResponse.StatusCode, new { Message = "Không thể lấy thông tin người dùng từ Mezon.", Details = userInfoError });
        }

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (userInfo.ValueKind != JsonValueKind.Object)
        {
            var userInfoRaw = await userInfoResponse.Content.ReadAsStringAsync();
            _logger.LogWarning("Unexpected Mezon userinfo payload: {Body}", userInfoRaw);
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "Userinfo payload is invalid.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Phản hồi userinfo từ Mezon không hợp lệ.", Details = userInfoRaw });
        }

        static string? GetStringByCandidate(JsonElement source, params string[] names)
        {
            foreach (var name in names)
            {
                if (!source.TryGetProperty(name, out var node))
                {
                    continue;
                }

                if (node.ValueKind == JsonValueKind.String)
                {
                    var value = node.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                if (node.ValueKind == JsonValueKind.Number)
                {
                    if (node.TryGetInt64(out var longValue))
                    {
                        return longValue.ToString();
                    }

                    if (node.TryGetDouble(out var doubleValue))
                    {
                        return doubleValue.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            return null;
        }

        var mezonUserId = GetStringByCandidate(userInfo, "user_id", "mezon_user_id", "sub", "id")
                            ?? GetStringByCandidate(tokenData, "user_id", "mezon_user_id", "sub", "id");
        var username = GetStringByCandidate(userInfo, "username", "preferred_username", "name");
        var displayName = GetStringByCandidate(userInfo, "display_name", "name") ?? username;
        _logger.LogInformation("Mezon userinfo received: {Payload}", userInfo.ToString());

        var avatarUrl = GetStringByCandidate(userInfo, "avatar", "avatar_url", "picture", "avatar_file", "profile_url", "image", "avatarUrl", "user_avatar")
                     ?? GetStringByCandidate(tokenData, "avatar", "avatar_url", "picture", "avatar_file", "profile_url", "image", "avatarUrl", "user_avatar");

        if (!string.IsNullOrWhiteSpace(avatarUrl) && !avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            avatarUrl = avatarUrl.StartsWith('/') 
                ? $"https://cdn.mezon.ai{avatarUrl}" 
                : $"https://cdn.mezon.ai/{avatarUrl}";
        }

        if (string.IsNullOrWhiteSpace(mezonUserId) && string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Mezon userinfo does not contain enough identity fields. Payload: {Payload}", userInfo.ToString());
            await WriteMezonAuditAsync(
                action: "mezon.login.failed",
                userId: null,
                title: "Mezon Login Failed",
                description: "Mezon userinfo does not contain enough identity fields.",
                status: "failed");
            return MezonCallbackResult.Failure(StatusCodes.Status502BadGateway, new { Message = "Không thể xác định người dùng từ Mezon." });
        }

        var now = DateTime.UtcNow;
        User? user = null;
        var isNewUser = false;

        if (!string.IsNullOrWhiteSpace(mezonUserId))
        {
            user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.MezonUserId == mezonUserId);
        }

        if (user is null)
        {
            isNewUser = true;
            var preferredName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : (!string.IsNullOrWhiteSpace(username) ? username : $"MezonUser_{mezonUserId?[..Math.Min(4, mezonUserId.Length)]}");

            user = new User
            {
                Id = Guid.NewGuid(),
                MezonUserId = mezonUserId ?? string.Empty,
                DisplayName = preferredName,
                AvatarUrl = avatarUrl,
                PreferredLocale = "vi-VN",
                Timezone = "Asia/Ho_Chi_Minh",
                Status = "active",
                Role = "learner",
                LastSeenAt = now
            };

            var profile = new LearnerProfile
            {
                UserId = user.Id,
                PlacementStatus = "pending"
            };
            user.Profile = profile;

            _dbContext.Users.Add(user);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                user.DisplayName = displayName;
            }

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
            }

            user.LastSeenAt = now;
            user.Status = "active";

            if (user.Profile == null)
            {
                user.Profile = new LearnerProfile
                {
                    UserId = user.Id,
                    PlacementStatus = "pending"
                };
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concurrency collision during user save for Mezon ID {MezonUserId}. Re-fetching user.", mezonUserId);
            _dbContext.ChangeTracker.Clear();

            user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.MezonUserId == mezonUserId);

            if (user == null)
            {
                throw;
            }
        }

        var accessTokenResult = _tokenService.CreateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshTokenValue);
        var refreshExpiration = _tokenService.GetRefreshTokenExpiration();

        // Store active refresh token hash in memory cache for token refresh and rotation
        var refreshCacheKey = $"refresh_token:{refreshTokenHash}";
        _memoryCache.Set(refreshCacheKey, user.Id.ToString(), refreshExpiration - DateTime.UtcNow);

        var userRole = user.Role ?? "learner";
        var roleNames = new List<string> { userRole };
        var hasSystemRole = userRole.Equals("admin", StringComparison.OrdinalIgnoreCase);
        var permissionNames = hasSystemRole
            ? new List<string> { "all.manage", "users.read", "users.write" }
            : new List<string> { "learning.study", "profile.edit" };

        await WriteMezonAuditAsync(
            action: "mezon.login.success",
            userId: user.Id,
            title: "Mezon Login Success",
            description: $"User '{user.DisplayName}' logged in via Mezon OAuth.",
            status: "success");

        return MezonCallbackResult.Success(new AuthResponseDto
        {
            Token = accessTokenResult.Token,
            RefreshToken = refreshTokenValue,
            ExpiresIn = accessTokenResult.ExpiresIn,
            IsNewUser = isNewUser,
            User = new

            {
                id = user.Id,
                mezonUserId = user.MezonUserId,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                role = user.Role,
                status = user.Status,
                preferredLocale = user.PreferredLocale,
                timezone = user.Timezone,
                lastSeenAt = user.LastSeenAt,
                profile = user.Profile != null ? new
                {
                    userId = user.Profile.UserId,
                    learningGoal = user.Profile.LearningGoal,
                    dailyCommitmentMinutes = user.Profile.DailyCommitmentMinutes,
                    proficiencyLevel = user.Profile.ProficiencyLevel,
                    placementStatus = user.Profile.PlacementStatus,
                    onboardingCompletedAt = user.Profile.OnboardingCompletedAt
                } : null
            },
            RoleName = roleNames,
            PermissionName = permissionNames,
            HasSystemRole = hasSystemRole
        });
    }

    private static string GetOAuthStateCacheKey(string state) => $"{OAuthStateCacheKeyPrefix}{state}";

    private async Task WriteMezonAuditAsync(string action, Guid? userId, string title, string description, string status)
    {
        try
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = userId,
                Action = action,
                EntityType = "User",
                EntityId = userId,
                Reason = $"{title}: {description}",
                Metadata = JsonSerializer.Serialize(new
                {
                    Title = title,
                    Description = description,
                    Status = status,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                }),
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write Mezon auth audit log.");
        }
    }

    private static string GenerateMezonState()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var state = new char[11];
        var buffer = RandomNumberGenerator.GetBytes(11);

        for (int i = 0; i < state.Length; i++)
        {
            state[i] = chars[buffer[i] % chars.Length];
        }

        return new string(state);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
