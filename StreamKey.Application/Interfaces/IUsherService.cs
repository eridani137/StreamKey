using StreamKey.Application.Results;

namespace StreamKey.Application.Interfaces;

public interface IUsherService
{
    Task<Result<string>> GetPlaylist(string username, string query);
}