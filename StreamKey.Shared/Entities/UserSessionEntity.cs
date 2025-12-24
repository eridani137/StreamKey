namespace StreamKey.Shared.Entities;

public class UserSessionEntity : BaseIntEntity
{
    public required string UserId { get; init; }
    public Guid SessionId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; } = TimeSpan.Zero;
}