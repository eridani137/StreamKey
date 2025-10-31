namespace StreamKey.Shared.Entities;

public class UserSessionEntity : BaseIntEntity
{
    public required string UserId { get; set; }
    public Guid SessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; } = TimeSpan.Zero;
}