using System.Net;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public class UsherService(HttpClient client) : IUsherService
{
    public async Task<Result<string>> GetPlaylist(string username, string query)
    {
        var url = $"api/channel/hls/{username}.m3u8{query}";

        try
        {
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<string>(Error.StreamNotFound);
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Result.Failure<string>(Error.PlaylistNotReceived($"Статус: {response.StatusCode}. Ответ: {errorContent}"));
            }
        
            var content = await response.Content.ReadAsStringAsync();
            return Result.Success(content);
        }
        catch (HttpRequestException httpEx)
        {
            if (httpEx.StatusCode is HttpStatusCode.NotFound)
            {
                return Result.Failure<string>(Error.StreamNotFound);
            }
            
            return Result.Failure<string>(Error.PlaylistNotReceived($"Статус: {httpEx.StatusCode}. Ошибка: {httpEx.Message}"));
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<string>(Error.Timeout);
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.UnexpectedError);
        }
    }
}