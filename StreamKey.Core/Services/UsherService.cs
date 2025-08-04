using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Results;

namespace StreamKey.Core.Services;

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
                var array = JArray.Parse(errorContent);
                foreach (var jToken in array)
                {
                    if (jToken is JObject obj)
                    {
                        obj.Remove("url");
                    }
                }
                return Result.Failure<string>(Error.PlaylistNotReceived(array.ToString(Formatting.None), (int)response.StatusCode));
            }
        
            var content = await response.Content.ReadAsStringAsync();
            return Result.Success(content);
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