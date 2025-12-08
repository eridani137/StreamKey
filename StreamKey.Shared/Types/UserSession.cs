namespace StreamKey.Shared.Types;

public class UserSession
{
    public string? UserId { get; set; }
    public required Guid SessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; }
}