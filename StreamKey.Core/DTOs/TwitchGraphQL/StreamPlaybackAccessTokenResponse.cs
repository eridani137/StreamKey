using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public record StreamPlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public StreamResponseData? Data { get; init; }
}

public record StreamResponseData
{
    [JsonPropertyName("streamPlaybackAccessToken")]
    public PlaybackAccessToken? StreamPlaybackAccessToken { get; init; }
}