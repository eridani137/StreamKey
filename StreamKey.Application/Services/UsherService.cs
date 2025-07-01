using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public interface IUsherService
{
    Task<Result<string>> GetHlsStreams(string username, PlaybackAccessTokenResponse accessToken);
}

public class UsherService(HttpClient client) : IUsherService
{
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");
    
    public async Task<Result<string>> GetHlsStreams(string username, PlaybackAccessTokenResponse accessToken)
    {
        var url = $"https://usher.ttvnw.net/api/channel/hls/{username}.m3u8?client_id={TwitchService.ClientId}&token={accessToken.Data!.StreamPlaybackAccessToken!.Value}&sig={accessToken.Data.StreamPlaybackAccessToken.Signature}&allow_source=true";
        var response = await client.GetStringAsync(url);

        return Result.Success(response);
    }
}