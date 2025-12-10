using System.Text.Json.Serialization;

namespace StreamKey.Shared.DTOs.Twitch;

public record StreamPlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public StreamResponseData? Data { get; init; }
}

public record StreamResponseData
{
    [JsonPropertyName("streamPlaybackAccessToken")]
    public PlaybackAccessToken? StreamPlaybackAccessToken { get; init; }
}