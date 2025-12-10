using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace StreamKey.Shared.DTOs.Telegram;

public record GetChatMemberResponse
{
    [JsonPropertyName("ok")]  public bool Ok { get; init; }
    
    [JsonPropertyName("result")] public ChatMemberResult? Result { get; init; }
}

public record ChatMemberResult
{
    [JsonPropertyName("user")] public ChatMemberUser? User { get; set; }
    
    [JsonPropertyName("status")] public ChatMemberStatus? Status { get; set; }
}

public record ChatMemberUser
{
    [JsonPropertyName("id")] public long Id { get; set; }
    
    [JsonPropertyName("is_bot")] public bool IsBot { get; set; }
    
    [JsonPropertyName("first_name")] public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("language_code")] public string LanguageCode { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatMemberStatus
{
    [EnumMember(Value = "creator")]
    Creator,
    
    [EnumMember(Value = "owner")]
    Owner,

    [EnumMember(Value = "administrator")]
    Administrator,
    
    [EnumMember(Value = "member")]
    Member,

    [EnumMember(Value = "restricted")]
    Restricted,

    [EnumMember(Value = "left")]
    Left,

    [EnumMember(Value = "kicked")]
    Kicked,
    
    [EnumMember(Value = "banned")]
    Banned
}