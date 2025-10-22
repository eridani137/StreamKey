using System.Net;
using System.Text;
using System.Web;
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

public class UsherService(
    IHttpClientFactory clientFactory,
    ITwitchService twitchService,
    ISettingsStorage settings,
    IMemoryCache cache) : IUsherService
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

        var uriBuilder = new UriBuilder(ApplicationConstants.UsherUrl)
        {
            Path = $"api/channel/hls/{username}.m3u8"
        };

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = ApplicationConstants.ClientId;
        query["token"] = tokenResponse?.Data?.StreamPlaybackAccessToken?.Value;
        query["sig"] = tokenResponse?.Data?.StreamPlaybackAccessToken?.Signature;
        query["allow_source"] = "true";
        query["fast_bread"] = "true";
        query["include_unavailable"] = "true";
        query["multigroup_video"] = "false";
        query["platform"] = "web";
        query["player_backend"] = "mediaplayer";
        query["playlist_include_framerate"] = "true";
        query["reassignments_supported"] = "true";
        query["supported_codecs"] = "av1,h265,h264";

        uriBuilder.Query = query.ToString();
        var url = uriBuilder.ToString();

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

                return Result.Failure<string>(Error.PlaylistNotReceived(array.ToString(Formatting.None),
                    (int)response.StatusCode));
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