using M3U8Parser;
using M3U8Parser.Tags.MultivariantPlaylist;
using StreamKey.Application.DTOs;
using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public interface IUsherService
{
    Task<Result<StreamResponseDto>> Get1080PStream(string username, PlaybackAccessTokenResponse accessToken);
}

public class UsherService(HttpClient client) : IUsherService
{
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");

    public async Task<Result<StreamResponseDto>> Get1080PStream(string username,
        PlaybackAccessTokenResponse accessToken)
    {
        var url =
            $"api/channel/hls/{username}.m3u8?client_id={TwitchService.ClientId}&token={accessToken.Data!.StreamPlaybackAccessToken!.Value}&sig={accessToken.Data.StreamPlaybackAccessToken.Signature}&allow_source=true";
        var response = await client.GetStringAsync(url);

        var playlist = MasterPlaylist.LoadFromText(response);
        if (playlist is null) return Result.Failure<StreamResponseDto>(Error.PlaylistNotReceived);

        var stream = playlist.Streams.FirstOrDefault(s =>
            s.Resolution.Height == 1080 && s.Resolution.Width == 1920);

        if (stream is null) return Result.Failure<StreamResponseDto>(Error.NotFound1080P);

        return Result.Success(new StreamResponseDto()
        {
            Source = stream.Uri
        });
    }
}