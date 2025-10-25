using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public class VideoPlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public VideoResponseData? Data { get; init; }
}

public class VideoResponseData
{
    [JsonPropertyName("videoPlaybackAccessToken")]
    public PlaybackAccessToken? VideoPlaybackAccessToken { get; init; }
}