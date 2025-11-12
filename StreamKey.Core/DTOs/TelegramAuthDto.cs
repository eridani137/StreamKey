using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs;

public class TelegramAuthDto
{
    [JsonPropertyName("auth_date")] public long AuthDate { get; set; }
    
    [JsonPropertyName("first_name")] public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("hash")] public required string Hash { get; set; }
    
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("photo_url")] public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
}