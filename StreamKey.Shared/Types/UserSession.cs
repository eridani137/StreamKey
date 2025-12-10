using MessagePack;

namespace StreamKey.Shared.Types;

[MessagePackObject]
public record UserSession
{
    [Key(0)] public string? UserId { get; set; }
    [Key(1)] public Guid SessionId { get; set; }
    [Key(2)] public DateTimeOffset StartedAt { get; set; }
    [Key(3)] public DateTimeOffset UpdatedAt { get; set; }
    [Key(4)] public TimeSpan AccumulatedTime { get; set; }
}