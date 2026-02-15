using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisPgSqlDemo.Data;
using RedisPgSqlDemo.Models;
using System.Text.Json;

namespace RedisPgSqlDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory; // Used to interface with Redis via IDistributedCache
    private readonly IDistributedCache _cache;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(AppDbContext context, IDistributedCache cache, ILogger<ClientsController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET: api/clients/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> GetClient(int id)
    {
        // 1. Пробуем прочитать из кэша
        var cacheKey = $"client_{id}";
        var cachedClient = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedClient))
        {
            _logger.LogInformation("Hit cache for client {Id}", id);
            return Ok(JsonSerializer.Deserialize<Client>(cachedClient));
        }

        // 2. При промахе читаем из БД
        var client = await _context.Clients.FindAsync(id);

        if (client == null)
        {
            return NotFound();
        }

        // 3. Сохраняем в кэш (Write-Behind / Lazy Loading )
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));
        _logger.LogInformation("Cached client {Id}", id);

        return Ok(client);
    }

    // POST: api/clients
    [HttpPost]
    public async Task<ActionResult<Client>> CreateClient(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        // Write-Through: Обновляем кэш сразу после записи в БД
        var cacheKey = $"client_{client.Id}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    // PUT: api/clients/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(int id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        _context.Entry(client).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        // Write-Through: Обновляем кэш сразу после записи в БД
        var cacheKey = $"client_{client.Id}";
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(client));

        return NoContent();
    }

    // DELETE: api/clients/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
        {
            return NotFound();
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        // Write-Through: Удаляем элемент из кэша сразу после изменения в БД
        var cacheKey = $"client_{id}";
        await _cache.RemoveAsync(cacheKey);

        return NoContent();
    }

    private bool ClientExists(int id)
    {
        return _context.Clients.Any(e => e.Id == id);
    }
}