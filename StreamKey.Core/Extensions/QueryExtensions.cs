using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpRequestMessage request)
        {
            var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
                ? auth.ToString()
                : ApplicationConstants.DefaultAuthorization;
            
            request.Headers.Add("Authorization", authorization);
            
            request.Headers.Add("x-device-id", TwitchExtensions.GenerateDeviceId());
        }
    }
}