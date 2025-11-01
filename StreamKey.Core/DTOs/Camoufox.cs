using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs;

public sealed record CamoufoxRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("wait_time")] int WaitTime);

public sealed record CamoufoxHtmlResponse(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("page_title")] string PageTitle,
    [property: JsonPropertyName("final_url")] string FinalUrl);