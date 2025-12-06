using Microsoft.AspNetCore.Http;
using StreamKey.Core.DTOs.TwitchGraphQL;

namespace StreamKey.Core.Abstractions;

public interface ITwitchService
{
    Task<StreamPlaybackAccessTokenResponse?> GetStreamAccessToken(string username, string deviceId, HttpContext context);
    Task<VideoPlaybackAccessTokenResponse?> GetVodAccessToken(string username, string deviceId, HttpContext context);
}