using Microsoft.AspNetCore.Http;
using StreamKey.Shared.DTOs.Twitch;

namespace StreamKey.Core.Abstractions;

public interface ITwitchService
{
    Task<PlaybackAccessToken?> GetStreamAccessToken(string username, string deviceId, HttpContext context);
    Task<PlaybackAccessToken?> GetVodAccessToken(string username, string deviceId, HttpContext context);
}