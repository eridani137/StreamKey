using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public record VideoPlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public VideoResponseData? Data { get; init; }
}

public record VideoResponseData
{
    [JsonPropertyName("videoPlaybackAccessToken")]
    public PlaybackAccessToken? VideoPlaybackAccessToken { get; init; }
}