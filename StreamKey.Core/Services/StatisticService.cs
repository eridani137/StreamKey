using System.Collections.Concurrent;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class StatisticService
{
    public ConcurrentQueue<ViewStatisticEntity> ViewStatisticQueue { get; } = new();
}