using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs.TwitchGraphQL;

public class PlaybackAccessTokenRequest
{
    [JsonPropertyName("operationName")]
    public string? OperationName { get; init; }

    [JsonPropertyName("query")]
    public string? Query { get; init; }

    [JsonPropertyName("variables")]
    public Variables? Variables { get; init; }
}

public class Variables
{
    [JsonPropertyName("isLive")]
    public bool IsLive { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("isVod")]
    public bool IsVod { get; set; }

    [JsonPropertyName("vodID")]
    public string? VodId { get; set; }

    [JsonPropertyName("playerType")]
    public string? PlayerType { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
}

public class AccessToken
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    // [JsonPropertyName("authorization")]
    // public Authorization? Authorization { get; set; }

    [JsonPropertyName("__typename")]
    public string? TypeName { get; set; }
}