namespace StreamKey.Infrastructure.Abstractions;

public interface ISettingsStorage
{
    Task<T?> GetSettingAsync<T>(string key, T defaultValue) where T : class;
    Task SetSettingAsync<T>(string key, T value) where T : class;

    Task<bool> GetBoolSettingAsync(string key, bool defaultValue);
    Task SetBoolSettingAsync(string key, bool value);
    
    Task RemoveSettingAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<IEnumerable<string>> GetAllKeysAsync();
}