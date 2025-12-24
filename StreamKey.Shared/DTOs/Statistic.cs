namespace StreamKey.Shared.DTOs;

public record BaseClicksStatistic
{
    public int ClickCount { get; set; }
    public int UniqueUsers { get; set; }
}

public record ChannelClicksStatistic : BaseClicksStatistic
{
    public required string ChannelName { get; set; }
    
}

public record ButtonClicksStatistic : BaseClicksStatistic
{
    public required string Link { get; set; }
}

public record ChannelViewStatistic
{
    public required string ChannelName { get; set; }
    public int ViewCount { get; set; }
}

public record UsersPerTimeStatistic
{
    public int UniqueUsersCount { get; set; }
}

public record UserTimeSpentStats(
    TimeSpan AverageTimeSpent,
    int TotalUsers,
    TimeSpan MedianTimeSpent,
    TimeSpan MinimumTimeSpent,
    TimeSpan MaximumTimeSpent);