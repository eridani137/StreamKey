namespace StreamKey.Shared.Types;

public record UserData
{
    public required Guid SessionId { get; set; }
}