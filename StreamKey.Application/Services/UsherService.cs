using System.Net;
using System.Text.RegularExpressions;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public partial class UsherService(HttpClient client) : IUsherService
{
    private static readonly Regex[] AdPatterns =
    [
        TwitchStitchedAdRegex(),
        StitchedAdRegex(),
        TvTwitchAdRegex(),
        AmazonAdRegex()
    ];

    public async Task<Result<string>> GetPlaylist(string username, string query)
    {
        var url = $"api/channel/hls/{username}.m3u8{query}";

        try
        {
            var response = await client.GetAsync(url);
        
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<string>(Error.StreamNotFound);
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            content = OptimizePlaylist(content);

            return Result.Success(content);
        }
        catch (HttpRequestException httpEx)
        {
            if (httpEx.StatusCode is HttpStatusCode.NotFound)
            {
                return Result.Failure<string>(Error.StreamNotFound);
            }
        
            return Result.Failure<string>(Error.UnexpectedError);
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<string>(Error.Timeout);
        }
        catch (Exception)
        {
            return Result.Failure<string>(Error.UnexpectedError);
        }
    }

    private static string OptimizePlaylist(string playlistContent)
    {
        playlistContent = AdPatterns.Aggregate(playlistContent, (current, pattern) => pattern.Replace(current, string.Empty));
        return playlistContent;
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