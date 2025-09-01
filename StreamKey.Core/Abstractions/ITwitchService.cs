using StreamKey.Core.DTOs.TwitchGraphQL;

namespace StreamKey.Core.Abstractions;

public interface ITwitchService
{
    Task<PlaybackAccessTokenResponse?> GetAccessToken(string username);
}