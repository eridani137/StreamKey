using StreamKey.Core.DTOs.TwitchGraphQL;

namespace StreamKey.Core.Abstractions;

public interface ITwitchService
{
    Task<PlaybackAccessTokenResponse?> GetStreamAccessToken(string username);
    Task<PlaybackAccessTokenResponse?> GetVodAccessToken(string username);
}