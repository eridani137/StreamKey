using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public class StreamPlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public StreamResponseData? Data { get; init; }
}

public class StreamResponseData
{
    [JsonPropertyName("streamPlaybackAccessToken")]
    public PlaybackAccessToken? StreamPlaybackAccessToken { get; init; }
}