using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisPgSqlDemo.Data;
using RedisPgSqlDemo.Models;
using RedisPgSqlDemo.Services;
using System.Text.Json;

namespace RedisPgSqlDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly IShardedDbContextFactory _contextFactory;
    private readonly IDistributedCache _cache;
    private readonly IShardingService _shardingService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IShardedDbContextFactory contextFactory,
                             IDistributedCache cache,
                             IShardingService shardingService,
                             ILogger<ClientsController> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _shardingService = shardingService;
        _logger = logger;
    }

    // GET: api/clients/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> GetClient(Guid id)
    {
        var cacheKey = $"client_{id}";
        var cachedClient = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedClient))
        {
            _logger.LogInformation("Hit cache for client {Id}", id);
            return Ok(JsonSerializer.Deserialize<Client>(cachedClient));
        }

        // Определяем шард, на котором находится клиент
        using var context = _contextFactory.CreateContext(id);

        var client = await context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        // Заполняем информацию о секции
        client.SetSectionInfo(_shardingService.GetSectionIndex(id));

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));
        _logger.LogInformation("Retrieved client {Id} from Shard {Shard}", id, _shardingService.GetShardIndex(id));

        return Ok(client);
    }

    // POST: api/clients
    [HttpPost]
    public async Task<ActionResult<Client>> CreateClient(Client client)
    {
        // Генерируем ID, если клиент его не прислал
        if (client.Id == Guid.Empty)
        {
            client.Id = Guid.NewGuid();
        }

        // Определяем целевой шард
        var shardIndex = _shardingService.GetShardIndex(client.Id);
        var sectionIndex = _shardingService.GetSectionIndex(client.Id);

        _logger.LogInformation("Creating client {Id} in Section {Section} on Shard {Shard}", client.Id, sectionIndex, shardIndex);

        // Соединяемся с нужным шардом
        using var context = _contextFactory.CreateContext(client.Id);

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        // Write-Through Cache
        client.SetSectionInfo(sectionIndex);
        var cacheKey = $"client_{client.Id}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    // PUT: api/clients/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, Client client)
    {
        if (id != client.Id) return BadRequest();

        using var context = _contextFactory.CreateContext(id);

        context.Entry(client).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Проверяем существование на конкретном шарде
            if (!await ClientExists(id, context)) return NotFound();
            else throw;
        }

        // Write-Through Cache
        var cacheKey = $"client_{id}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));

        return NoContent();
    }

    // DELETE: api/clients/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        using var context = _contextFactory.CreateContext(id);

        var client = await context.Clients.FindAsync(id);
        if (client == null) return NotFound();

        context.Clients.Remove(client);
        await context.SaveChangesAsync();

        var cacheKey = $"client_{id}";
        await _cache.RemoveAsync(cacheKey);

        return NoContent();
    }

    private async Task<bool> ClientExists(Guid id, AppDbContext context)
    {
        return await context.Clients.AnyAsync(e => e.Id == id);
    }
}