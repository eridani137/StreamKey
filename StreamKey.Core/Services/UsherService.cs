using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Core.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Twitch;

namespace StreamKey.Core.Services;

public class UsherService(
    IHttpClientFactory clientFactory,
    ITwitchService twitchService,
    IMemoryCache cache
) : IUsherService
{
    private const string StreamTokenKey = "UsherStreamKey";
    private const string VodTokenKey = "UsherVodKey";

    private static string GetStreamKey(string username, string deviceId)
    {
        return $"{StreamTokenKey}:{username}:{deviceId}";
    }

    private static string GetVodKey(string vodId, string deviceId)
    {
        return $"{VodTokenKey}:{vodId}:{deviceId}";
    }

    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(2);

    public async Task<HttpResponseMessage?> GetStreamPlaylist(string username, string deviceId,
        HttpContext context)
    {
        var cacheKey = GetStreamKey(username, deviceId);
        
        if (!cache.TryGetValue(cacheKey, out StreamPlaybackAccessTokenResponse? tokenResponse) || tokenResponse is null)
        {
            tokenResponse = await twitchService.GetStreamAccessToken(username, deviceId, context);
            if (tokenResponse is not null)
            {
                cache.Set(cacheKey, tokenResponse, AbsoluteExpiration);
            }
        }

        if (tokenResponse?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            tokenResponse?.Data?.StreamPlaybackAccessToken?.Value is null)
        {
            return null;
        }

        var uriBuilder = new UriBuilder(ApplicationConstants.UsherUrl)
        {
            Path = $"api/v2/channel/hls/{username}.m3u8"
        };

        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var (key, value) in context.Request.Query)
        {
            if (key.Equals("auth")) continue;
            query[key] = value;
        }

        query["sig"] = tokenResponse.Data.StreamPlaybackAccessToken.Signature;
        query["token"] = tokenResponse.Data.StreamPlaybackAccessToken.Value;

        uriBuilder.Query = query.ToString();

        var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);
        return await client.GetAsync(uriBuilder.ToString(), HttpCompletionOption.ResponseHeadersRead);
    }

    public async Task<HttpResponseMessage?> GetVodPlaylist(string vodId, string deviceId, HttpContext context)
    {
        var cacheKey = GetVodKey(vodId, deviceId);
        
        if (!cache.TryGetValue(cacheKey, out VideoPlaybackAccessTokenResponse? tokenResponse) ||
            tokenResponse is null)
        {
            tokenResponse = await twitchService.GetVodAccessToken(vodId, deviceId, context);
            if (tokenResponse is not null)
            {
                cache.Set(cacheKey, tokenResponse, AbsoluteExpiration);
            }
        }

        if (tokenResponse?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            tokenResponse.Data.VideoPlaybackAccessToken.Value is null)
        {
            return null;
        }

        var uriBuilder = new UriBuilder(ApplicationConstants.UsherUrl)
        {
            Path = $"vod/{vodId}.m3u8"
        };

        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var (key, value) in context.Request.Query)
        {
            if (key.Equals("vod_id") || key.Equals("auth")) continue;
            query[key] = value;
        }

        query["client_id"] = ApplicationConstants.ClientId;
        query["token"] = tokenResponse.Data?.VideoPlaybackAccessToken?.Value;
        query["sig"] = tokenResponse.Data?.VideoPlaybackAccessToken?.Signature;

        uriBuilder.Query = query.ToString();

        var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);
        return await client.GetAsync(uriBuilder.ToString(), HttpCompletionOption.ResponseHeadersRead);
    }
}