using System.IdentityModel.Tokens.Jwt;
using Lingual.Infrastructure.Cache.Redis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lingual.Modules.Identity.Infrastructure.Services;

public class TokenRevocationService(
    IMemoryCache memoryCache,
    IServiceProvider serviceProvider,
    ILogger<TokenRevocationService> logger) : ITokenRevocationService
{
    private const string RevokedTokenPrefix = "revoked_token:";

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return;

            var jwtToken = handler.ReadJwtToken(token);
            var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti" || c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrWhiteSpace(jti)) return;

            var validTo = jwtToken.ValidTo;
            var ttl = validTo > DateTime.UtcNow 
                ? validTo - DateTime.UtcNow 
                : TimeSpan.FromMinutes(5);

            var cacheKey = $"{RevokedTokenPrefix}{jti}";

            // 1. In-memory cache (instant)
            memoryCache.Set(cacheKey, true, ttl);

            // 2. Distributed Redis cache (if connected)
            var connectionMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
            if (connectionMultiplexer != null && connectionMultiplexer.IsConnected)
            {
                var redisService = serviceProvider.GetService<IRedisService>();
                if (redisService != null)
                {
                    try
                    {
                        await redisService.SetAsync(cacheKey, true, ttl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to persist revoked token to Redis, fallback to memory cache.");
                    }
                }
            }

            logger.LogInformation("Token {Jti} successfully revoked with TTL {Ttl}", jti, ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to revoke token.");
        }
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti)) return false;

        var cacheKey = $"{RevokedTokenPrefix}{jti}";

        // 1. Check in-memory cache
        if (memoryCache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        // 2. Check Redis cache (only if connected)
        var connectionMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
        if (connectionMultiplexer != null && connectionMultiplexer.IsConnected)
        {
            var redisService = serviceProvider.GetService<IRedisService>();
            if (redisService != null)
            {
                try
                {
                    var isRevokedInRedis = await redisService.GetAsync<bool?>(cacheKey);
                    if (isRevokedInRedis.HasValue && isRevokedInRedis.Value)
                    {
                        memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(5));
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to check token revocation in Redis.");
                }
            }
        }

        return false;
    }
}
