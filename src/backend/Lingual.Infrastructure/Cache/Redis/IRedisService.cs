namespace Lingual.Infrastructure.Cache.Redis;

public interface IRedisService
{
    T? Get<T>(string key);

    Task<T?> GetAsync<T>(string key);

    T GetOrSet<T>(string key, Func<T> valueFactory);

    T GetOrSet<T>(string key, Func<T> valueFactory, TimeSpan expiration);

    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> valueFactory, TimeSpan? expiration = null);

    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    T HashGetOrSet<T>(string key, string hashKey, Func<T> valueFactory);

    Task<T> HashGetOrSetAsync<T>(string key, string hashKey, Func<Task<T>> valueFactory);

    IEnumerable<string> GetKeys(string pattern);

    IEnumerable<T> GetValues<T>(string key);

    bool RemoveAllKeys(string pattern = "*");

    Task<bool> RemoveAllKeysAsync(string pattern = "*");

    void Remove(string key);

    Task<bool> RemoveAsync(string key);

    void Reset();
}