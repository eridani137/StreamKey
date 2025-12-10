namespace StreamKey.Shared.Types;

public record ChannelViewStatistic
{
    public required string ChannelName { get; set; }
    public int ViewCount { get; set; }
}