using System.Text.Json.Serialization;

namespace StreamKey.Shared.DTOs;

public record CamoufoxRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("wait_time")] int WaitTime);