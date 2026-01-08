using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    private const string Scheme = "OAuth";
    
    extension(IQueryCollection query)
    {
        public void AddQueryAuthAndDeviceId(HttpRequestMessage request, string deviceId)
        {
            const string scheme = "OAuth";
            
            var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
                ? $"{scheme} {auth}"
                : $"{scheme} {ApplicationConstants.DefaultAuthorization}";

            request.Headers.Add("authorization", authorization);

            request.Headers.Add("x-device-id", deviceId);
        }
        
        public static void AddQueryDeviceId(HttpRequestMessage request, string deviceId)
        {
            request.Headers.Add("authorization", $"{Scheme} {ApplicationConstants.DefaultAuthorization}");

            request.Headers.Add("x-device-id", deviceId);
        }
    }
}