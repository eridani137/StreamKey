using System.Net;
using System.Text.RegularExpressions;
using M3U8Parser;
using StreamKey.Application.DTOs;
using StreamKey.Application.DTOs.TwitchGraphQL;
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

    public async Task<Result<StreamResponseDto>> GetPlaylist(string username, PlaybackAccessTokenResponse accessToken)
    {
        var url = $"api/channel/hls/{username}.m3u8?client_id={StaticData.ClientId}&token={accessToken.Data!.StreamPlaybackAccessToken!.Value}&sig={accessToken.Data.StreamPlaybackAccessToken.Signature}&allow_source=true";

        try
        {
            var response = await client.GetAsync(url);
        
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<StreamResponseDto>(Error.StreamNotFound);
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            content = OptimizePlaylist(content);

            var stream1080PUrl = Extract1080PStreamUrl(content);

            if (string.IsNullOrEmpty(stream1080PUrl))
            {
                return Result.Failure<StreamResponseDto>(Error.NullValue);
            }

            return Result.Success(new StreamResponseDto
            {
                Source = stream1080PUrl
            });
        }
        catch (HttpRequestException httpEx)
        {
            if (httpEx.StatusCode is HttpStatusCode.NotFound)
            {
                return Result.Failure<StreamResponseDto>(Error.StreamNotFound);
            }
        
            return Result.Failure<StreamResponseDto>(Error.UnexpectedError);
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<StreamResponseDto>(Error.Timeout);
        }
        catch (Exception)
        {
            return Result.Failure<StreamResponseDto>(Error.UnexpectedError);
        }
    }

    private static string OptimizePlaylist(string playlistContent)
    {
        playlistContent = AdPatterns.Aggregate(playlistContent, (current, pattern) => pattern.Replace(current, string.Empty));
        // playlistContent = ApplyLowLatencySettings(playlistContent);
        playlistContent = FilterTo1080pOnly(playlistContent);

        return playlistContent;
    }

    private static string FilterTo1080pOnly(string playlistContent)
    {
        var streamSections = StreamSectionRegex().Matches(playlistContent);
        if (streamSections.Count <= 1) return playlistContent;

        var sb = new System.Text.StringBuilder();

        var headerEndIndex = playlistContent.IndexOf("#EXT-X-MEDIA:TYPE=VIDEO", StringComparison.Ordinal);
        if (headerEndIndex > 0)
        {
            sb.Append(playlistContent[..headerEndIndex]);
        }

        foreach (Match section in streamSections)
        {
            var sectionText = section.Value;
            if (!sectionText.Contains("1920x1080") && !sectionText.Contains("source")) continue;
            
            sb.Append(sectionText);
            break;
        }

        return sb.ToString();
    }

    private static string Extract1080PStreamUrl(string playlistContent)
    {
        var match = StreamUrlRegex().Match(playlistContent);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ApplyLowLatencySettings(string playlistContent)
    {
        const string serverControlReplacement =
            "#EXT-X-SERVER-CONTROL:CAN-BLOCK-RELOAD=YES,PART-HOLD-BACK=1.0,HOLD-BACK=3.0\n";

        if (ServerControlRegex().IsMatch(playlistContent))
        {
            return ServerControlRegex().Replace(playlistContent, serverControlReplacement);
        }

        return ExtM3URegex().Replace(playlistContent, "#EXTM3U\n" + serverControlReplacement.TrimEnd(), 1);
    }

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*twitch-stitched-ad.*\n", RegexOptions.Compiled)]
    private static partial Regex TwitchStitchedAdRegex();

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*stitched-ad-.*\n", RegexOptions.Compiled)]
    private static partial Regex StitchedAdRegex();

    [GeneratedRegex(@"#EXT-X-DATERANGE:.*X-TV-TWITCH-AD-.*\n", RegexOptions.Compiled)]
    private static partial Regex TvTwitchAdRegex();

    [GeneratedRegex(@"#EXTINF:.*Amazon.*\n.*\n", RegexOptions.Compiled)]
    private static partial Regex AmazonAdRegex();

    [GeneratedRegex(@"#EXT-X-SERVER-CONTROL:.*\n")]
    private static partial Regex ServerControlRegex();

    [GeneratedRegex(@"#EXTM3U")]
    private static partial Regex ExtM3URegex();

    [GeneratedRegex(@"(#EXT-X-MEDIA:TYPE=VIDEO.*?)(#EXT-X-MEDIA:TYPE=VIDEO|\Z)", RegexOptions.Singleline)]
    private static partial Regex StreamSectionRegex();

    [GeneratedRegex(@"RESOLUTION=1920x1080.*\n(https://.*\.m3u8)")]
    private static partial Regex StreamUrlRegex();
}