namespace StreamKey.Shared.Entities;

public class ChannelEntity : BaseEntity
{
    public required string Name { get; set; }
    public required int Position { get; set; }
}