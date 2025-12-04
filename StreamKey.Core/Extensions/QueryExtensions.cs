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
            
            var deviceId = query.TryGetValue("device-id", out var device) && !string.IsNullOrEmpty(device)
                ? device.ToString()
                : ApplicationConstants.DefaultDeviceId;
            
            request.Headers.Add("device-id", deviceId);
        }

        public void AddQueryAuth(HttpClient client)
        {
            var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
                ? auth.ToString()
                : ApplicationConstants.DefaultAuthorization;

            client.DefaultRequestHeaders.Add("Authorization", authorization);

            var deviceId = query.TryGetValue("device-id", out var device) && !string.IsNullOrEmpty(device)
                ? device.ToString()
                : ApplicationConstants.DefaultDeviceId;

            client.DefaultRequestHeaders.Add("device-id", deviceId);
        }
    }
}