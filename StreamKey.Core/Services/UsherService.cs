using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class UsherService(IHttpClientFactory clientFactory, ISettingsStorage settings) : IUsherService
{
    public async Task<Result<string>> GetPlaylist(string username, string query)
    {
        var url = $"api/channel/hls/{username}.m3u8{query}";

        try
        {
            using var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);
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