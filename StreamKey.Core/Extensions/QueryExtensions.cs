using Microsoft.AspNetCore.Http;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class QueryExtensions
{
    extension(IQueryCollection query)
    {
        public void AddQueryAuth(HttpRequestMessage request)
        {
            // var authorization = query.TryGetValue("auth", out var auth) && !string.IsNullOrEmpty(auth)
            //     ? auth.ToString()
            //     : ApplicationConstants.DefaultAuthorization;

            var authorization = ApplicationConstants.DefaultAuthorization;

            request.Headers.Add("Authorization", authorization);

            var deviceId = authorization == ApplicationConstants.DefaultAuthorization
                ? ApplicationConstants.DefaultDeviceId
                : TwitchExtensions.GenerateDeviceId();

            request.Headers.Add("x-device-id", deviceId);
        }
    }
}