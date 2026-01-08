namespace StreamKey.Shared.Entities;

public class ClickButtonEntity : BaseIntEntity
{
    public required string Link { get; init; }
    public required string UserId { get; init; }
    public DateTime DateTime { get; init; }
    public ButtonPosition Position { get; init; }
}