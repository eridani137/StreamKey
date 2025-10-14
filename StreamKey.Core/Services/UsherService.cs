using System.Net;
using System.Text;
using M3U8Parser;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs.TwitchGraphQL;
using StreamKey.Core.Extensions;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class UsherService(IHttpClientFactory clientFactory, ITwitchService twitchService, ISettingsStorage settings, IMemoryCache cache) : IUsherService
{
    public async Task<Result<string>> GetPlaylist(string username, string query)
    {
        var url = $"api/channel/hls/{username}.m3u8{query}";

        return await GetPlaylist(url);
    }

    public async Task<Result<string>> GetServerPlaylist(string username)
    {
        if (!cache.TryGetValue(username, out PlaybackAccessTokenResponse? tokenResponse) || tokenResponse is null)
        {
            tokenResponse = await twitchService.GetAccessToken(username);
            if (tokenResponse is not null)
            {
                cache.Set(username, tokenResponse, TimeSpan.FromMinutes(3));
            }
        }

        if (tokenResponse is null)
        {
            return Result.Failure<string>(Error.ServerTokenNotFound);
        }
        
        var url = $"api/channel/hls/{username}.m3u8?client_id={ApplicationConstants.ClientId}&token={tokenResponse.Data!.StreamPlaybackAccessToken!.Value}&sig={tokenResponse.Data.StreamPlaybackAccessToken.Signature}&allow_source=true";

        return await GetPlaylist(url);
    }

    private async Task<Result<string>> GetPlaylist(string url)
    {
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

            // if (await settings.GetBoolSettingAsync(ApplicationConstants.RemoveAds, true))
            // {
            //     content = await RemoveAds(content);
            // }
            
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

    private async Task<string> RemoveAds(string content)
    {
        var masterPlaylist = MasterPlaylist.LoadFromText(content);
        
        var mediaStream = masterPlaylist.Streams.FirstOrDefault(s => s.Video is "1080p60" or "1080p");
        if (mediaStream is null) return content;
        
        using var client = clientFactory.CreateClient(ApplicationConstants.CleanClientName);
        var response = await client.GetAsync(mediaStream.Uri);

        var mediaPlaylist = await response.Content.ReadAsStringAsync();
        mediaPlaylist = mediaPlaylist.RemoveAds();
        
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "files");

        var fileName = Guid.CreateVersion7().ToString("N");
        var filePath = Path.Combine(uploads, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        var bytes = Encoding.UTF8.GetBytes(mediaPlaylist);
        await stream.WriteAsync(bytes);

        mediaStream.Uri = $"https://service.streamkey.ru/files/{fileName}";

        return masterPlaylist.ToString();
    }
}