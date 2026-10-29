namespace Lingual.Infrastructure.Cache.Redis;

public sealed class RedisSettings
{
    public string Prefix { get; set; } = "Lingual";
    public string Url { get; set; } = "localhost:6379";
    public string? Password { get; set; }
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public int ConnectRetry { get; set; } = 3;
    public bool AbortOnConnectFail { get; set; } = false;
    public int DeltaBackOff { get; set; } = 2000;
    public bool Ssl { get; set; } = false;
    public int RedisDefaultSlidingExpirationInSecond { get; set; } = 3600;

    public string GetConnectionString()
    {
        var conn = Url;
        if (!string.IsNullOrWhiteSpace(Password))
        {
            conn += $",password={Password}";
        }
        conn += $",ssl={Ssl},abortConnect={AbortOnConnectFail},connectTimeout={ConnectTimeout},syncTimeout={SyncTimeout}";
        return conn;
    }
}
