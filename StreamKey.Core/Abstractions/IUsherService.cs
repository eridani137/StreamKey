using Microsoft.AspNetCore.Http;
using StreamKey.Core.Results;

namespace StreamKey.Core.Abstractions;

public interface IUsherService
{
    Task<Result<HttpResponseMessage>> GetStreamPlaylist(string username, string deviceId, HttpContext context);
    Task<Result<HttpResponseMessage>> GetVodPlaylist(string vodId, string deviceId, HttpContext context);
}