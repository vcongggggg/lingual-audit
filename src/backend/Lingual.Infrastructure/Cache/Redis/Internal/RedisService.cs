using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Lingual.Infrastructure.Cache.Redis.Internal;

public sealed class RedisService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisSettings> options) : IRedisService
{
    private const string GET_KEYS_LUA_SCRIPT = """
                                                   local pattern = ARGV[1]
                                                   local keys = redis.call('KEYS', pattern)
                                                   return keys
                                               """;

    private const string CLEAR_CACHE_LUA_SCRIPT = """
                                                      local pattern = ARGV[1]
                                                      for _,k in ipairs(redis.call('KEYS', pattern)) do
                                                          redis.call('DEL', k)
                                                      end
                                                  """;

    private readonly RedisSettings _redisCacheSetting = options.Value;

    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public T GetOrSet<T>(string key, Func<T> valueFactory)
        => GetOrSet(key, valueFactory,
            TimeSpan.FromSeconds(_redisCacheSetting.RedisDefaultSlidingExpirationInSecond));

    public T GetOrSet<T>(string key, Func<T> valueFactory, TimeSpan expiration)
    {
        Guard.Against.NullOrEmpty(key);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";

        var cachedValue = Database.StringGet(keyWithPrefix);
        if (!string.IsNullOrEmpty(cachedValue)) return GetByteToObject<T>(cachedValue);

        var newValue = valueFactory();
        if (newValue is not null) Database.StringSet(keyWithPrefix, JsonSerializer.Serialize(newValue), expiration);

        return newValue;
    }

    public T? Get<T>(string key)
    {
        Guard.Against.NullOrEmpty(key);
        Guard.Against.NullOrEmpty(_redisCacheSetting.Prefix);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";

        var cachedValue = Database.StringGet(keyWithPrefix);
        return !string.IsNullOrEmpty(cachedValue)
            ? GetByteToObject<T>(cachedValue)
            : default;
    }

    public T HashGetOrSet<T>(string key, string hashKey, Func<T> valueFactory)
    {
        Guard.Against.NullOrEmpty(key);
        Guard.Against.NullOrEmpty(hashKey);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";
        var value = Database.HashGet(keyWithPrefix, hashKey.ToLower());

        if (!string.IsNullOrEmpty(value)) return GetByteToObject<T>(value);

        var newValue = valueFactory();
        if (newValue is not null)
            Database.HashSet(keyWithPrefix, hashKey.ToLower(),
                JsonSerializer.Serialize(newValue));

        return newValue;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        Guard.Against.NullOrEmpty(key);
        Guard.Against.NullOrEmpty(_redisCacheSetting.Prefix);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";
        var cachedValue = await Database.StringGetAsync(keyWithPrefix);
        return !cachedValue.IsNullOrEmpty
            ? GetByteToObject<T>(cachedValue)
            : default;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> valueFactory, TimeSpan? expiration = null)
    {
        Guard.Against.NullOrEmpty(key);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";
        var cachedValue = await Database.StringGetAsync(keyWithPrefix);
        if (!cachedValue.IsNullOrEmpty) return GetByteToObject<T>(cachedValue);

        var newValue = await valueFactory();
        if (newValue is not null)
        {
            var exp = expiration ?? TimeSpan.FromSeconds(_redisCacheSetting.RedisDefaultSlidingExpirationInSecond);
            await Database.StringSetAsync(keyWithPrefix, JsonSerializer.Serialize(newValue), exp);
        }

        return newValue;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        Guard.Against.NullOrEmpty(key);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";
        var exp = expiration ?? TimeSpan.FromSeconds(_redisCacheSetting.RedisDefaultSlidingExpirationInSecond);
        await Database.StringSetAsync(keyWithPrefix, JsonSerializer.Serialize(value), exp);
    }

    public async Task<T> HashGetOrSetAsync<T>(string key, string hashKey, Func<Task<T>> valueFactory)
    {
        Guard.Against.NullOrEmpty(key);
        Guard.Against.NullOrEmpty(hashKey);

        var keyWithPrefix = $"{_redisCacheSetting.Prefix}:{key}";
        var value = await Database.HashGetAsync(keyWithPrefix, hashKey.ToLower());

        if (!value.IsNullOrEmpty) return GetByteToObject<T>(value);

        var newValue = await valueFactory();
        if (newValue is not null)
        {
            await Database.HashSetAsync(keyWithPrefix, hashKey.ToLower(), JsonSerializer.Serialize(newValue));
        }

        return newValue;
    }

    public async Task<bool> RemoveAsync(string key)
    {
        Guard.Against.NullOrEmpty(key);
        return await Database.KeyDeleteAsync($"{_redisCacheSetting.Prefix}:{key}");
    }

    public async Task<bool> RemoveAllKeysAsync(string pattern = "*")
    {
        var keys = GetKeys($"{_redisCacheSetting.Prefix}:{pattern}");
        var tasks = keys.Select(key => Database.KeyDeleteAsync(key));
        var results = await Task.WhenAll(tasks);
        return results.All(r => r);
    }

    public IEnumerable<string> GetKeys(string pattern)
        => ((RedisResult[])Database.ScriptEvaluate(GET_KEYS_LUA_SCRIPT, values: [pattern])!)
            .Where(x => x.ToString().StartsWith(_redisCacheSetting.Prefix))
            .Select(x => x.ToString())
            .ToArray();

    public IEnumerable<T> GetValues<T>(string key)
        => Database.HashGetAll($"{_redisCacheSetting.Prefix}:{key}").Select(x => GetByteToObject<T>(x.Value));

    public bool RemoveAllKeys(string pattern = "*")
    {
        var succeed = true;

        var keys = GetKeys($"{_redisCacheSetting.Prefix}:{pattern}");
        foreach (var key in keys) succeed = Database.KeyDelete(key);

        return succeed;
    }

    public void Remove(string key) => Database.KeyDelete($"{_redisCacheSetting.Prefix}:{key}");

    public void Reset()
        => Database.ScriptEvaluate(
            CLEAR_CACHE_LUA_SCRIPT,
            values: [_redisCacheSetting.Prefix + ":*"],
            flags: CommandFlags.FireAndForget);

    private static T GetByteToObject<T>(RedisValue value)
    {
        if (value.IsNullOrEmpty) throw new InvalidOperationException("RedisValue is null or empty.");
        var result = JsonSerializer.Deserialize<T>((string)value!);
        return result is null ? throw new InvalidOperationException("Deserialization failed.") : result;
    }
}