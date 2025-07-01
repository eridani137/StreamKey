using System.Text.RegularExpressions;
using M3U8Parser;
using StreamKey.Application.DTOs;
using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public interface IUsherService
{
    Task<Result<StreamResponseDto>> Get1080PStream(string username, PlaybackAccessTokenResponse accessToken);
}

public partial class UsherService(HttpClient client) : IUsherService
{
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");
    private static readonly Regex[] AdPatterns =
    [
        TwitchStitchedAdRegex(),
        StitchedAdRegex(),
        TvTwitchAdRegex(),
        AmazonAdRegex()
    ];
    
    public async Task<Result<StreamResponseDto>> Get1080PStream(string username, PlaybackAccessTokenResponse accessToken)
    {
        var url =
            $"api/channel/hls/{username}.m3u8?client_id={TwitchService.ClientId}&token={accessToken.Data!.StreamPlaybackAccessToken!.Value}&sig={accessToken.Data.StreamPlaybackAccessToken.Signature}&allow_source=true";
        var response = await client.GetStringAsync(url);
        response = RemoveAdsFromPlaylist(response);
        
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

    private static string RemoveAdsFromPlaylist(string playlistContent)
    {
        return AdPatterns.Aggregate(playlistContent, (current, pattern) => pattern.Replace(current, string.Empty));
    }

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*twitch-stitched-ad.*\n", RegexOptions.Compiled)]
    private static partial Regex TwitchStitchedAdRegex();

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*stitched-ad-.*\n", RegexOptions.Compiled)]
    private static partial Regex StitchedAdRegex();

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*X-TV-TWITCH-AD-.*\n", RegexOptions.Compiled)]
    private static partial Regex TvTwitchAdRegex();

    [GeneratedRegex(@"#EXTINF:.*Amazon.*\n.*\n", RegexOptions.Compiled)]
    private static partial Regex AmazonAdRegex();
}