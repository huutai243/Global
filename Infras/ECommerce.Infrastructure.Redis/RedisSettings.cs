namespace ECommerce.Infrastructure.Redis;

public sealed class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";

    public int DefaultExpirationInMinutes { get; set; } = 10;
}