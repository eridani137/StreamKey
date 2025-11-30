namespace StreamKey.Core.Types;

public class UserSession
{
    public string? UserId { get; set; }
    public required Guid SessionId { get; set; }
    public TimeSpan AccumulatedTime { get; set; }
}