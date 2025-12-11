using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreamKey.Shared;

internal static class JsonNatsDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}