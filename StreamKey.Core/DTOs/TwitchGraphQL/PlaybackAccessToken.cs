using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public class PlaybackAccessToken
{
    [JsonPropertyName("value")] public string? Value { get; init; }

    [JsonPropertyName("signature")] public string? Signature { get; init; }
}