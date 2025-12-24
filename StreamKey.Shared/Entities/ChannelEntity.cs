using StreamKey.Shared.DTOs;

namespace StreamKey.Shared.Entities;

public class ChannelEntity : BaseGuidEntity
{
    public required string Name { get; init; }
    public required int Position { get; set; }
    public ChannelInfo? Info { get; set; }
}