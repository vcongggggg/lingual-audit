using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Lingual.Modules.Identity.Contracts.DTOs;
using Lingual.Modules.Identity.Infrastructure.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public class MezonAuthService(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment,
    IMemoryCache memoryCache,
    ILogger<MezonAuthService> logger) : IMezonAuthService
{
    private static readonly TimeSpan OAuthStateTtl = TimeSpan.FromMinutes(5);
    private const string OAuthStateCacheKeyPrefix = "mezon_oauth_state:";

    private MezonOAuth2Options GetOptions()
    {
        var options = new MezonOAuth2Options();
        var section = configuration.GetSection(MezonOAuth2Options.SectionName);
        if (section != null && section.GetChildren().Any())
        {
            section.Bind(options);
        }

        var clientId = configuration["MezonOAuth2:ClientId"] ?? configuration["Mezon:AppId"] ?? configuration["MEZON_APP_ID"];
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            options.ClientId = clientId;
        }

        var clientSecret = configuration["MezonOAuth2:ClientSecret"] ?? configuration["Mezon:AppSecret"] ?? configuration["MEZON_APP_SECRET"];
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            options.ClientSecret = clientSecret;
        }

        var authorizeUrl = configuration["MezonOAuth2:AuthorizeUrl"];
        if (!string.IsNullOrWhiteSpace(authorizeUrl))
        {
            options.AuthorizeUrl = authorizeUrl;
        }

        var tokenUrl = configuration["MezonOAuth2:TokenUrl"];
        if (!string.IsNullOrWhiteSpace(tokenUrl))
        {
            options.TokenUrl = tokenUrl;
        }

        var userInfoUrl = configuration["MezonOAuth2:UserInfoUrl"];
        if (!string.IsNullOrWhiteSpace(userInfoUrl))
        {
            options.UserInfoUrl = userInfoUrl;
        }

        var redirectUri = configuration["MezonOAuth2:RedirectUri"] ?? configuration["Mezon:RedirectUri"];
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            options.RedirectUri = redirectUri;
        }

        var scope = configuration["MezonOAuth2:Scope"];
        if (!string.IsNullOrWhiteSpace(scope))
        {
            options.Scope = scope;
        }

        if (string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            options.RedirectUri = "http://localhost:3000/oauth/mezon/callback";
        }

        return options;
    }

    public MezonAuthorizeUrlDto GetAuthorizeUrl(string? customRedirectUri = null)
    {
        var options = GetOptions();
        var redirectUri = !string.IsNullOrWhiteSpace(customRedirectUri) ? customRedirectUri : options.RedirectUri;

        var state = GenerateOAuthState();
        memoryCache.Set(GetOAuthStateCacheKey(state), true, OAuthStateTtl);

        var authUrl =
            $"{options.AuthorizeUrl}?client_id={Uri.EscapeDataString(options.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(options.Scope)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return new MezonAuthorizeUrlDto
        {
            AuthorizeUrl = authUrl,
            State = state
        };
    }

    public async Task<MezonUserInfo?> ValidateAndGetUserInfoAsync(
        LoginWithMezonRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Verify OAuth state if provided
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            var stateKey = GetOAuthStateCacheKey(request.State);
            if (!memoryCache.TryGetValue(stateKey, out _))
            {
                logger.LogWarning("Invalid or expired OAuth state provided: {State}", request.State);
            }
            else
            {
                memoryCache.Remove(stateKey);
            }
        }

        // 2. If Authorization Code is provided, exchange for token with Mezon
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeUserInfo = await TryExchangeCodeAndGetUserInfoAsync(request, cancellationToken);
            if (codeUserInfo != null)
            {
                return codeUserInfo;
            }
        }

        // 3. If explicit Access Token is provided, query Mezon UserInfo endpoint
        var token = request.EffectiveToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var oauthUserInfo = await TryGetUserInfoFromOAuth2Async(token, cancellationToken);
            if (oauthUserInfo != null)
            {
                return oauthUserInfo;
            }

            // Attempt reading claims if token is in JWT format
            var jwtUserInfo = TryReadUserInfoFromJwt(token);
            if (jwtUserInfo != null)
            {
                return jwtUserInfo;
            }
        }

        // 4. Development / Local / Mock fallback or Bot context with MezonUserId
        var isDevOrTest = environment.IsDevelopment()
            || configuration["APP_ENV"] == "local"
            || (!string.IsNullOrEmpty(token) && (token.StartsWith("mock_", StringComparison.OrdinalIgnoreCase) 
                                              || token.StartsWith("dev_", StringComparison.OrdinalIgnoreCase) 
                                              || token.StartsWith("test_", StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(request.MezonUserId))
        {
            logger.LogInformation("Using contextual Mezon user ID {MezonUserId}", request.MezonUserId);
            return new MezonUserInfo(
                request.MezonUserId,
                !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName : $"MezonUser_{request.MezonUserId[^Math.Min(4, request.MezonUserId.Length)..]}",
                request.AvatarUrl
            );
        }

        if (isDevOrTest && !string.IsNullOrWhiteSpace(token))
        {
            logger.LogInformation("Creating development mock user identity for dev token {Token}", token);
            var devId = Math.Abs(token.GetHashCode()).ToString();
            return new MezonUserInfo(
                $"dev_mezon_{devId}",
                request.DisplayName ?? $"Dev Learner ({devId[^Math.Min(4, devId.Length)..]})",
                request.AvatarUrl ?? "https://mezon.ai/default-avatar.png"
            );
        }

        logger.LogWarning("Mezon SSO authentication failed: unable to validate token against Mezon OAuth2 endpoint.");
        return null;
    }

    private async Task<MezonUserInfo?> TryExchangeCodeAndGetUserInfoAsync(LoginWithMezonRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var options = GetOptions();

            if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
            {
                logger.LogWarning("Cannot exchange Mezon code: ClientId or ClientSecret is missing in configuration.");
                return null;
            }

            var redirectUri = !string.IsNullOrWhiteSpace(request.RedirectUri) ? request.RedirectUri : options.RedirectUri;

            var payload = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = request.Code!,
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["redirect_uri"] = redirectUri
            };

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                payload["state"] = request.State;
            }

            using var tokenReq = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(payload)
            };

            var tokenRes = await httpClient.SendAsync(tokenReq, cancellationToken);
            if (!tokenRes.IsSuccessStatusCode)
            {
                var errorBody = await tokenRes.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Mezon token exchange failed. Status: {StatusCode}, Body: {Body}", tokenRes.StatusCode, errorBody);
                return null;
            }

            var tokenJson = await tokenRes.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(tokenJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var tokenProp))
            {
                var accessToken = tokenProp.GetString();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var userInfo = await TryGetUserInfoFromOAuth2Async(accessToken, cancellationToken);
                    if (userInfo != null)
                    {
                        return userInfo;
                    }

                    // Fallback to identity properties inside token response itself if userinfo endpoint fails
                    var mezonIdInToken = GetStringByCandidate(root, "user_id", "mezon_user_id", "sub", "id");
                    if (!string.IsNullOrWhiteSpace(mezonIdInToken))
                    {
                        var name = GetStringByCandidate(root, "display_name", "name", "username") ?? $"User_{mezonIdInToken}";
                        var avatar = GetStringByCandidate(root, "avatar", "picture", "avatar_url");
                        return new MezonUserInfo(mezonIdInToken, name, avatar);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while exchanging authorization code with Mezon OAuth2");
        }

        return null;
    }

    private async Task<MezonUserInfo?> TryGetUserInfoFromOAuth2Async(string token, CancellationToken cancellationToken)
    {
        try
        {
            var options = GetOptions();
            using var req = new HttpRequestMessage(HttpMethod.Get, options.UserInfoUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Mezon userinfo endpoint responded with status {StatusCode}", res.StatusCode);
                return null;
            }

            var content = await res.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var mezonUserId = GetStringByCandidate(root, "user_id", "mezon_user_id", "sub", "id");
            if (string.IsNullOrWhiteSpace(mezonUserId))
            {
                logger.LogWarning("Mezon userinfo response does not contain user ID candidate property");
                return null;
            }

            var displayName = GetStringByCandidate(root, "display_name", "name", "username", "preferred_username") ?? $"User_{mezonUserId}";
            var avatarUrl = GetStringByCandidate(root, "avatar", "picture", "avatar_url");

            return new MezonUserInfo(mezonUserId, displayName, avatarUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while contacting Mezon OAuth2 userinfo endpoint");
            return null;
        }
    }

    private MezonUserInfo? TryReadUserInfoFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            var jwtToken = handler.ReadJwtToken(token);
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "user_id" || c.Type == "id")?.Value;
            if (string.IsNullOrWhiteSpace(sub)) return null;

            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == "display_name" || c.Type == "username")?.Value ?? $"User_{sub}";
            var avatar = jwtToken.Claims.FirstOrDefault(c => c.Type == "picture" || c.Type == "avatar" || c.Type == "avatar_url")?.Value;

            return new MezonUserInfo(sub, name, avatar);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetStringByCandidate(JsonElement source, params string[] names)
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
                    return longValue.ToString(CultureInfo.InvariantCulture);
                }

                if (node.TryGetDouble(out var doubleValue))
                {
                    return doubleValue.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        return null;
    }

    private static string GenerateOAuthState()
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

    private static string GetOAuthStateCacheKey(string state) => $"{OAuthStateCacheKeyPrefix}{state}";
}
