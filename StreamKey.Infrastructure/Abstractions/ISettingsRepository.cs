namespace StreamKey.Infrastructure.Abstractions;

public interface ISettingsRepository
{
    Task<Dictionary<string, object>> GetAll();
    Task<T> SetValue<T>(string key, T value);
    Task<T?> GetValue<T>(string key);
    Task Remove(string key);
}