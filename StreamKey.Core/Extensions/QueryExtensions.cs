using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpClient client)
        {
            // var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
            //     ? auth.ToString()
            //     : ApplicationConstants.DefaultAuthorization;
            
            var authorization = ApplicationConstants.DefaultAuthorization;

            client.DefaultRequestHeaders.Add("Authorization", authorization);

            var deviceId = ApplicationConstants.DefaultDeviceId;
            
            // var deviceId = query.TryGetValue("x-device-id", out var device) && !string.IsNullOrEmpty(device)
            //     ? device.ToString()
            //     : ApplicationConstants.DefaultDeviceId;

            client.DefaultRequestHeaders.Add("x-device-id", deviceId);

            client.DefaultRequestHeaders.Add("Client-ID", ApplicationConstants.ClientId);
        }
    }
}