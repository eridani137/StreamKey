using Microsoft.AspNetCore.Http;
using StreamKey.Core.Results;

namespace StreamKey.Core.Abstractions;

public interface IUsherService
{
    Task<Result<string>> GetStreamPlaylist(string username, string deviceId, HttpContext context);
    Task<Result<string>> GetVodPlaylist(string vodId, string deviceId, HttpContext context);
}