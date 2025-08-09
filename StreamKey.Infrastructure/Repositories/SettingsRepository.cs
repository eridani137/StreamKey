using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class SettingsRepository(ApplicationDbContext context)
    : BaseRepository<SettingsEntity>(context), ISettingsRepository
{
    public async Task<Dictionary<string, object>?> GetAll()
    {
        var settings = await GetSet()
            .Select(s => new
            {
                s.Key, 
                s.Value
            })
            .ToListAsync();
    
        return settings
            .Where(s => !string.IsNullOrEmpty(s.Value))
            .ToDictionary(s => s.Key, object (s) => s.Value);
    }

    public async Task<T> SetValue<T>(string key, T value)
    {
        var jsonValue = JsonSerializer.Serialize(value);
        var existing = await GetSet().FirstOrDefaultAsync(s => s.Key == key);

        if (existing is not null)
        {
            existing.Value = jsonValue;
            Update(existing);
        }
        else
        {
            var newSetting = new SettingsEntity
            {
                Key = key,
                Value = jsonValue
            };
            await Add(newSetting);
        }
        await Save();
        
        return value;
    }

    public async Task<T?> GetValue<T>(string key)
    {
        var setting = await GetSet().FirstOrDefaultAsync(s => s.Key == key);
        
        return setting?.Value is null
            ? default 
            : JsonSerializer.Deserialize<T>(setting.Value);
    }

    public async Task Remove(string key)
    {
        var setting = GetSet().FirstOrDefault(s => s.Key == key);
        if (setting is not null)
        {
            Delete(setting);
            await Save();
        }
    }
}