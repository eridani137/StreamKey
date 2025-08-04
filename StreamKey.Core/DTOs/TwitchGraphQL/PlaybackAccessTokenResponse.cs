using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public class PlaybackAccessTokenResponse
{
    [JsonPropertyName("data")] public ResponseData? Data { get; init; }
}

public class ResponseData
{
    [JsonPropertyName("streamPlaybackAccessToken")]
    public PlaybackAccessToken? StreamPlaybackAccessToken { get; init; }
}

public class PlaybackAccessToken
{
    [JsonPropertyName("value")] public string? Value { get; init; }

    [JsonPropertyName("signature")] public string? Signature { get; init; }
}