using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class UsherService(HttpClient client, ISettingsStorage settings) : IUsherService
{
    public async Task<Result<string>> GetPlaylist(string username, string query)
    {
        var url = $"api/channel/hls/{username}.m3u8{query}";

        try
        {
            using var activity = Activity.Current;
            activity?.SetTag("operation", "stream_check");
            activity?.SetTag("username", username);
            
            var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                activity?.SetTag("expected_not_found", "true");
                activity?.SetStatus(ActivityStatusCode.Ok, "Stream not found - expected behavior");
                
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

            if (await settings.GetBoolSettingAsync(ApplicationConstants.RemoveAds, true))
            {
                content = content.RemoveAds();
            }
            
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