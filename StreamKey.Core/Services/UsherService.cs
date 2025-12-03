using System.Net;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs.TwitchGraphQL;
using StreamKey.Core.Results;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class UsherService(
    IHttpClientFactory clientFactory,
    ITwitchService twitchService,
    IMemoryCache cache
) : IUsherService
{
    public async Task<Result<string>> GetStreamPlaylist(string username, HttpContext context)
    {
        // if (!cache.TryGetValue(username, out StreamPlaybackAccessTokenResponse? tokenResponse) || tokenResponse is null)
        // {
        //     tokenResponse = await twitchService.GetStreamAccessToken(username, context);
        //     if (tokenResponse is not null)
        //     {
        //         cache.Set(username, tokenResponse, TimeSpan.FromMinutes(1));
        //     }
        // } // TODO

        var tokenResponse = await twitchService.GetStreamAccessToken(username, context);

        if (tokenResponse?.Data is null)
        {
            return Result.Failure<string>(Error.ServerTokenNotFound);
        }

        var uriBuilder = new UriBuilder(ApplicationConstants.UsherUrl)
        {
            Path = $"api/v2/channel/hls/{username}.m3u8"
        };
        
        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var (key, value) in context.Request.Query)
        {
            if (key.Equals("auth") || key.Equals("device")) continue;
            query[key] = value;
        }

        query["sig"] = tokenResponse.Data?.StreamPlaybackAccessToken?.Signature;
        query["token"] = tokenResponse.Data?.StreamPlaybackAccessToken?.Value;

        uriBuilder.Query = query.ToString();
        var url = uriBuilder.ToString();

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

    public async Task<Result<string>> GetVodPlaylist(string vodId, HttpContext context)
    {
        // if (!cache.TryGetValue(vodId, out VideoPlaybackAccessTokenResponse? tokenResponse) || tokenResponse is null)
        // {
        //     tokenResponse = await twitchService.GetVodAccessToken(vodId, context);
        //     if (tokenResponse is not null)
        //     {
        //         cache.Set(vodId, tokenResponse, TimeSpan.FromMinutes(1));
        //     }
        // } // TODO

        var tokenResponse = await twitchService.GetVodAccessToken(vodId, context);

        if (tokenResponse?.Data is null)
        {
            return Result.Failure<string>(Error.ServerTokenNotFound);
        }

        var uriBuilder = new UriBuilder(ApplicationConstants.UsherUrl)
        {
            Path = $"vod/{vodId}.m3u8"
        };

        var query = HttpUtility.ParseQueryString(string.Empty);
        
        foreach (var (key, value) in context.Request.Query)
        {
            if (key.Equals("vod_id")) continue;
            query[key] = value;
        }
        
        query["client_id"] = ApplicationConstants.ClientId;
        query["token"] = tokenResponse.Data?.VideoPlaybackAccessToken?.Value;
        query["sig"] = tokenResponse.Data?.VideoPlaybackAccessToken?.Signature;

        uriBuilder.Query = query.ToString();
        var url = uriBuilder.ToString();

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