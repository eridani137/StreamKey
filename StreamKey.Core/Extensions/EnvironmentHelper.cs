namespace StreamKey.Core.Extensions;

public static class EnvironmentHelper
{
    public static string? GetOtlpHeaders() =>
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");

    public static string GetSeqEndpoint() =>
        Environment.GetEnvironmentVariable("SEQ_ENDPOINT")!;
    
    public static string? GetSeqApiKey() => ExtractApiKeyFromHeaders();

    private static string? ExtractApiKeyFromHeaders()
    {
        var headers = GetOtlpHeaders();
        if (string.IsNullOrEmpty(headers)) return null;
        
        var headerParts = headers.Split('=', 2);
        return headerParts is ["X-Seq-ApiKey", _] ? headerParts[1] : null;
    }
}