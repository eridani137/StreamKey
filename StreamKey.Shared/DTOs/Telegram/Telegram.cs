using System.Text.Json.Serialization;
using MessagePack;
using ProtoBuf;

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

[ProtoContract]
[MessagePackObject]
public record TelegramUserDto
{
    [ProtoMember(1)]
    [Key("id")]
    public long Id { get; set; }

    [ProtoMember(2)]
    [Key("username")]
    public string Username { get; set; } = string.Empty;

    [ProtoMember(3)]
    [Key("photo_url")]
    public string PhotoUrl { get; set; } = string.Empty;

    [ProtoMember(4)]
    [Key("is_chat_member")]
    public bool IsChatMember { get; set; }
}

[ProtoContract]
[MessagePackObject]
public record TelegramUserRequest
{
    [ProtoMember(1)] [Key("userId")] public required long UserId { get; set; }
    [ProtoMember(2)] [Key("userHash")] public required string UserHash { get; set; }
}

[ProtoContract]
[MessagePackObject]
public record CheckMemberRequest
{
    [ProtoMember(1)]
    [Key("userId")]
    public required long UserId { get; set; }
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