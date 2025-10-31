namespace StreamKey.Shared.Types;

public record UserTimeSpentStats(TimeSpan AverageTimeSpent, int TotalUsers, TimeSpan MedianTimeSpent);