using System.Text.Json.Serialization;
using MessagePack;

namespace StreamKey.Shared.DTOs.Telegram;

public record TelegramAuthDto
{
    [JsonPropertyName("auth_date")] public long AuthDate { get; init; }

    [JsonPropertyName("first_name")] public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("hash")] public string Hash { get; init; } = string.Empty;

    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("photo_url")] public string PhotoUrl { get; init; } = string.Empty;

    [JsonPropertyName("username")] public string Username { get; init; } = string.Empty;
}

[MessagePackObject]
public record TelegramUserDto
{
    [Key("id")] public long Id { get; set; }

    [Key("username")] public string Username { get; set; } = string.Empty;

    [Key("photo_url")] public string PhotoUrl { get; set; } = string.Empty;

    [Key("is_chat_member")] public bool IsChatMember { get; set; }
}

[MessagePackObject]
public record TelegramUserRequest
{
    [Key("userId")] public long UserId { get; set; }
}

[MessagePackObject]
public record CheckMemberRequest
{
    [Key("userId")] public long UserId { get; set; }
}

public record TelegramAuthDtoWithSessionId : TelegramAuthDto
{
    public Guid SessionId { get; init; }

    public TelegramAuthDtoWithSessionId(TelegramAuthDto dto, Guid sessionId)
    {
        AuthDate = dto.AuthDate;
        FirstName = dto.FirstName;
        Hash = dto.Hash;
        Id = dto.Id;
        PhotoUrl = dto.PhotoUrl;
        Username = dto.Username;

        SessionId = sessionId;
    }
}