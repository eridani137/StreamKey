using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpRequestMessage request, string deviceId)
        {
            var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
                ? $"OAuth {auth}"
                : "undefined";

            request.Headers.Add("Authorization", authorization);

            request.Headers.Add("x-device-id", deviceId);
        }
    }
}