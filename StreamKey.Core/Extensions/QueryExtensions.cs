using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "OAuth",
                query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
                    ? auth
                    : ApplicationConstants.DefaultAuthorization
            );

            client.DefaultRequestHeaders.Add("x-device-id",
                query.TryGetValue("device-id", out var deviceId) && !string.IsNullOrEmpty(deviceId)
                    ? deviceId
                    : ApplicationConstants.DefaultDeviceId
            );
        }
    }
}