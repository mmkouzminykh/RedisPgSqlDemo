using Microsoft.EntityFrameworkCore;
using RedisPgSqlDemo.Services;

namespace RedisPgSqlDemo.Data
{
    
    public interface IShardedDbContextFactory
    {
        AppDbContext CreateContext(Guid clientId);
    }

    public class ShardedDbContextFactory : IShardedDbContextFactory
    {
        private readonly IShardingService _shardingService;
        private readonly IConfiguration _configuration;

        public ShardedDbContextFactory(IShardingService shardingService, IConfiguration configuration)
        {
            _shardingService = shardingService;
            _configuration = configuration;
        }

        public AppDbContext CreateContext(Guid clientId)
        {
            var connectionString = _shardingService.GetShardConnectionString(clientId);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}