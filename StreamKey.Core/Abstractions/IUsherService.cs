using Microsoft.AspNetCore.Http;

namespace StreamKey.Core.Abstractions;

public interface IUsherService
{
    Task<HttpResponseMessage?> GetStreamPlaylist(string username, string deviceId, HttpContext context);
    Task<HttpResponseMessage?> GetVodPlaylist(string vodId, string deviceId, HttpContext context);
}