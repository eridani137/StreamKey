namespace StreamKey.Shared.Types;

public record ChannelClicksStatistic
{
    public required string ChannelName { get; set; }
    public int ClickCount { get; set; }
    public int UniqueUsers { get; set; }
}