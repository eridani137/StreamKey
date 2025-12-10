using System.Text.Json.Serialization;

namespace StreamKey.Shared.DTOs.Twitch;

public record PlaybackAccessToken
{
    [JsonPropertyName("value")] public string? Value { get; init; }

    [JsonPropertyName("signature")] public string? Signature { get; init; }
}