using StreamKey.Shared.DTOs;

namespace StreamKey.Shared.Abstractions;

public interface IStatisticStore
{
    Task SaveClickAsync(ClickChannel click);
}