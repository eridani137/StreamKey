using MessagePack;

namespace StreamKey.Shared.Types;

[MessagePackObject]
public record UserSessionMessage
{
    [Key(0)] public required string ConnectionId { get; init; }

    [Key(1)] public UserSession? Session { get; set; }
}