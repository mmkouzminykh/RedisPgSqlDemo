using Microsoft.EntityFrameworkCore;
using RedisPgSqlDemo.Data;
using RedisPgSqlDemo.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Register Sharding Logic
builder.Services.AddSingleton<IShardingService, ShardingService>();
builder.Services.AddScoped<IShardedDbContextFactory, ShardedDbContextFactory>();

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "ShopApi_";
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Инициализация БД
// В реальной системе тут будут проверки и прогон скриптов миграции
// Для простого примера мы предполагаем, что "shop_shard_0", "shop_shard_1", "shop_shard_2" существуют

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();