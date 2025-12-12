using System.Web;
using Microsoft.AspNetCore.Http;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Results;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class UsherService(
    IHttpClientFactory clientFactory,
    ITwitchService twitchService
) : IUsherService
{
    public async Task<Result<HttpResponseMessage>> GetStreamPlaylist(string username, string deviceId, HttpContext context)
    {
        var tokenResponse = await twitchService.GetStreamAccessToken(username, deviceId, context);

        if (tokenResponse?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            tokenResponse?.Data?.StreamPlaybackAccessToken?.Value is null)
        {
            return Result.Failure<HttpResponseMessage>(Error.ServerTokenNotFound);
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
        var url = uriBuilder.ToString();

        try
        {
            var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            return Result.Success(response);
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<HttpResponseMessage>(Error.Timeout);
        }
        catch (Exception)
        {
            return Result.Failure<HttpResponseMessage>(Error.UnexpectedError);
        }
    }

    public async Task<Result<HttpResponseMessage>> GetVodPlaylist(string vodId, string deviceId, HttpContext context)
    {
        var tokenResponse = await twitchService.GetVodAccessToken(vodId, deviceId, context);

        if (tokenResponse?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            tokenResponse.Data.VideoPlaybackAccessToken.Value is null)
        {
            return Result.Failure<HttpResponseMessage>(Error.ServerTokenNotFound);
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
        var url = uriBuilder.ToString();

        try
        {
            var client = clientFactory.CreateClient(ApplicationConstants.UsherClientName);
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            return Result.Success(response);
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<HttpResponseMessage>(Error.Timeout);
        }
        catch (Exception)
        {
            return Result.Failure<HttpResponseMessage>(Error.UnexpectedError);
        }
    }
}