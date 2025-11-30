namespace StreamKey.Core.Types;

public class UserData
{
    public required string UserId { get; set; }
    public required Guid SessionId { get; set; }
}