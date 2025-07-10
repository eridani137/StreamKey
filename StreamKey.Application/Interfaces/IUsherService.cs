using Newtonsoft.Json.Linq;
using StreamKey.Application.Results;

namespace StreamKey.Application.Interfaces;

public interface IUsherService
{
    Task<string> ModifyToken(JObject  tokenValue);
    Task<Result<string>> GetPlaylist(string username, string query);
}