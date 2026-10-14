using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Lingual.Infrastructure.Cache.Redis;
using Lingual.Infrastructure.Cache.Redis.Internal;
using StackExchange.Redis;

namespace Lingual.Infrastructure.Cache;

public static class Extension
{
    public static IServiceCollection AddRedisCache(
        this IHostApplicationBuilder builder,
        Action<RedisSettings>? setupAction = null)
    {
        if (builder.Services.Contains(ServiceDescriptor.Singleton<IRedisService, RedisService>()))
            return builder.Services;

        var redisSettings = new RedisSettings();
        var redisCacheSection = builder.Configuration.GetSection(nameof(RedisSettings));
        redisCacheSection.Bind(redisSettings);

        // Fallback to ConnectionStrings:Redis if configured
        var connStr = builder.Configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            redisSettings.Url = connStr;
        }

        setupAction?.Invoke(redisSettings);
        builder.Services.Configure<RedisSettings>(options =>
        {
            options.Prefix = redisSettings.Prefix;
            options.Url = redisSettings.Url;
            options.Password = redisSettings.Password;
            options.ConnectTimeout = redisSettings.ConnectTimeout;
            options.SyncTimeout = redisSettings.SyncTimeout;
            options.ConnectRetry = redisSettings.ConnectRetry;
            options.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
            options.DeltaBackOff = redisSettings.DeltaBackOff;
            options.Ssl = redisSettings.Ssl;
            options.RedisDefaultSlidingExpirationInSecond = redisSettings.RedisDefaultSlidingExpirationInSecond;
        });

        var configurationOptions = GetRedisConfigurationOptions(redisSettings);

        // Register shared singleton IConnectionMultiplexer
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configurationOptions));

        // Configure Distributed Cache using the same options
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = $"{redisSettings.Prefix}:";
            options.ConfigurationOptions = configurationOptions;
        });

        builder.Services.AddSingleton<IRedisService, RedisService>();

        return builder.Services;
    }

    private static ConfigurationOptions GetRedisConfigurationOptions(RedisSettings redisSettings)
    {
        ConfigurationOptions configurationOptions = new()
        {
            ConnectTimeout = redisSettings.ConnectTimeout,
            SyncTimeout = redisSettings.SyncTimeout,
            ConnectRetry = redisSettings.ConnectRetry,
            AbortOnConnectFail = redisSettings.AbortOnConnectFail,
            ReconnectRetryPolicy = new ExponentialRetry(redisSettings.DeltaBackOff),
            KeepAlive = 5,
            Ssl = redisSettings.Ssl
        };

        if (!string.IsNullOrWhiteSpace(redisSettings.Password))
        {
            configurationOptions.Password = redisSettings.Password;
        }

        foreach (var endpoint in redisSettings.Url.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            configurationOptions.EndPoints.Add(endpoint);
        }

        return configurationOptions;
    }
}