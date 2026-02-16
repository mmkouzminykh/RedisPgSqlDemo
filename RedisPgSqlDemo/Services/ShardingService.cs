namespace RedisPgSqlDemo.Services;

public interface IShardingService
{
    int GetShardIndex(Guid clientId);
    int GetSectionIndex(Guid clientId);
    string GetShardConnectionString(Guid clientId);
}

public class ShardingService : IShardingService
{
    private readonly IConfiguration _configuration;

    public ShardingService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // 1. Получаем хэш ID
    // 2. Вычисляем остаток от деления для получения номера секции Section (0 to 8)
    public int GetSectionIndex(Guid clientId)
    {
        //лучше использовать более продвинутые библиотеки
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(clientId.ToByteArray());
        
        int hashInt = BitConverter.ToInt32(hash, 0);

        // получаем номер секции
        int section = (Math.Abs(hashInt) % 9);
        return section;
    }

    // Отображаем номер секции в шард, тоже самым простым способом
    // Section 0, 3, 6 -> Shard 0
    // Section 1, 4, 7 -> Shard 1
    // Section 2, 5, 8 -> Shard 2
    public int GetShardIndex(Guid clientId)
    {
        int section = GetSectionIndex(clientId);
        return section % 3;
    }

    public string GetShardConnectionString(Guid clientId)
    {
        int shardIndex = GetShardIndex(clientId);
        // Matches "Shard_0", "Shard_1" in appsettings.json
        string connectionName = $"Shard_{shardIndex}";
        return _configuration.GetConnectionString(connectionName)
               ?? throw new Exception($"Connection string for Shard {shardIndex} not found.");
    }
}