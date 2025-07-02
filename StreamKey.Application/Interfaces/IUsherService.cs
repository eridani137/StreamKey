using StreamKey.Application.DTOs;
using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Results;

namespace StreamKey.Application.Interfaces;

public interface IUsherService
{
    Task<Result<StreamResponseDto>> GetPlaylist(string username, PlaybackAccessTokenResponse accessToken);
}