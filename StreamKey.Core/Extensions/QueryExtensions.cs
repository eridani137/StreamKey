using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpRequestMessage request)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("OAuth", ApplicationConstants.DefaultAuthorization);

            request.Headers.Add("x-device-id", ApplicationConstants.DefaultDeviceId);

            // request.Headers.Authorization = new AuthenticationHeaderValue(
            //     "OAuth",
            //     query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
            //         ? auth
            //         : ApplicationConstants.DefaultAuthorization
            // );
            //
            // request.Headers.Add("x-device-id",
            //     query.TryGetValue("device-id", out var deviceId) && !string.IsNullOrEmpty(deviceId)
            //         ? deviceId
            //         : ApplicationConstants.DefaultDeviceId
            // );
        }
    }
}