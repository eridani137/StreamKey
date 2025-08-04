using StreamKey.Core.Results;

namespace StreamKey.Core.Abstractions;

public interface IUsherService
{
    Task<Result<string>> GetPlaylist(string username, string query);
}