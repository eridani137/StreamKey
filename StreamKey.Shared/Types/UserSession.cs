namespace StreamKey.Shared.Types;

public class UserSession
{
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; } = TimeSpan.Zero;
}