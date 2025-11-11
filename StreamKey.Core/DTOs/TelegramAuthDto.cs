using System.Text.Json.Serialization;

namespace StreamKey.Core.DTOs;

public class TelegramAuthDto
{
    public long Id { get; set; }
    
    [JsonPropertyName("auth_date")] 
    public long AuthDate { get; set; }
    
    [JsonPropertyName("first_name")] 
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("last_name")] 
    public string LastName { get; set; }  = string.Empty;
    
    public required string Hash { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("photo_url")] 
    public string PhotoUrl { get; set; }  = string.Empty;
    
}