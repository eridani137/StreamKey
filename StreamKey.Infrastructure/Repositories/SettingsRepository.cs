using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Extensions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class SettingsRepository(ApplicationDbContext context)
    : BaseRepository<SettingsEntity>(context), ISettingsRepository
{
    public async Task<Dictionary<string, object?>?> GetAll()
    {
        var settings = await GetSet()
            .Select(s => new
            {
                s.Key, 
                s.Value
            })
            .ToListAsync();
    
        var result = new Dictionary<string, object?>();
        
        foreach (var setting in settings.Where(s => !string.IsNullOrEmpty(s.Value)))
        {
            var deserializedValue = setting.Value.DeserializeValue();
            result[setting.Key] = deserializedValue;
        }
        
        return result;
    }

    public async Task<T> SetValue<T>(string key, T value)
    {
        string stringValue;
        var type = typeof(T);
    
        if (type.IsSimpleType())
        {
            stringValue = value?.ToString() ?? string.Empty;
        }
        else
        {
            stringValue = JsonSerializer.Serialize(value);
        }
    
        var existing = await GetSet().FirstOrDefaultAsync(s => s.Key == key);

        if (existing is not null)
        {
            existing.Value = stringValue;
            Update(existing);
        }
        else
        {
            var newSetting = new SettingsEntity
            {
                Key = key,
                Value = stringValue
            };
            await Add(newSetting);
        }
        await Save();
    
        return value;
    }


    public async Task<T?> GetValue<T>(string key)
    {
        var setting = await GetSet().FirstOrDefaultAsync(s => s.Key == key);
    
        if (setting?.Value is null) return default;
    
        var type = typeof(T);

        if (!type.IsSimpleType()) return JsonSerializer.Deserialize<T>(setting.Value);
        if (type == typeof(string)) return (T)(object)setting.Value;
                
        return (T)Convert.ChangeType(setting.Value, type);

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