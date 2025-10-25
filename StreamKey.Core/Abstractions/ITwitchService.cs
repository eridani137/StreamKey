using StreamKey.Core.DTOs.TwitchGraphQL;

namespace StreamKey.Core.Abstractions;

public interface ITwitchService
{
    Task<StreamPlaybackAccessTokenResponse?> GetStreamAccessToken(string username);
    Task<VideoPlaybackAccessTokenResponse?> GetVodAccessToken(string username);
}