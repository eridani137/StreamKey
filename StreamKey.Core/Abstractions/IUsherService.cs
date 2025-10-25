using StreamKey.Core.Results;

namespace StreamKey.Core.Abstractions;

public interface IUsherService
{
    Task<Result<string>> GetStreamPlaylist(string username);
    Task<Result<string>> GetVodPlaylist(string vodId);
}