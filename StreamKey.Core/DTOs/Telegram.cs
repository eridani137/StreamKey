using System.Text.Json.Serialization;
using MessagePack;

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

[MessagePackObject]
public class TelegramUserDto
{
    [Key("id")]
    [JsonPropertyName("id")] 
    public long Id { get; set; }

    [Key("username")]
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    [Key("photo_url")]
    [JsonPropertyName("photo_url")]
    public required string PhotoUrl { get; set; }

    [Key("is_chat_member")]
    [JsonPropertyName("is_chat_member")]
    public bool IsChatMember { get; set; }
}

public class TelegramUserRequest
{
    public required long UserId { get; set; }
    public required string UserHash { get; set; }
}

public class CheckMemberRequest
{
    public required long UserId { get; set; }
}