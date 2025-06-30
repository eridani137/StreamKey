using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StreamKey.Application.Interfaces;

namespace StreamKey.Application.Services;

public class StreamService(ILogger<StreamService> logger) : IStreamService
{
    public async Task<string?> GetSource(string username)
    {
        const string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
        var payload =
            $"{{\"operationName\":\"PlaybackAccessToken_Template\",\"query\":\"query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {{  streamPlaybackAccessToken(channelName: $login, params: {{platform: $platform, playerBackend: \\\"mediaplayer\\\", playerType: $playerType}}) @include(if: $isLive) {{    value    signature   authorization {{ isForbidden forbiddenReasonCode }}   __typename  }}  videoPlaybackAccessToken(id: $vodID, params: {{platform: $platform, playerBackend: \\\"mediaplayer\\\", playerType: $playerType}}) @include(if: $isVod) {{    value    signature   __typename  }}}}\",\"variables\": {{\"isLive\": true,\"login\": \"{username}\",\"isVod\": false,\"vodID\": \"\",\"playerType\": \"site\",\"platform\": \"web\"}}}}";

        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://gql.twitch.tv/gql"),
            Headers =
            {
                { "Accept", "*/*" },
                { "Accept-Language", "en-US" },
                { "Client-ID", clientId },
                { "Origin", "https://www.twitch.tv" },
                { "Referer", "https://www.twitch.tv/" },
                { "sec-ch-ua", """Not)A;Brand";v="8", "Chromium";v="138", "Google Chrome";v="138""" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", """Windows""" },
                { "Sec-Fetch-Dest", "empty" },
                { "Sec-Fetch-Mode", "cors" },
                { "Sec-Fetch-Site", "same-site" },
                {
                    "User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36"
                },
            },
            Content = new StringContent(payload)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        
        var obj = JObject.Parse(body);
        var token = obj.SelectToken("..data.streamPlaybackAccessToken.value")?.ToString();
        var sig = obj.SelectToken("..data.streamPlaybackAccessToken.signature")?.ToString();

        if (token is null || sig is null)
        {
            logger.LogError("Ошибка при получении значений из JSON: {JSON}", obj.ToString());
            
            return null;
        }
        
        var url = $"https://usher.ttvnw.net/api/channel/hls/{username}.m3u8?client_id={clientId}&token={token}&sig={sig}&allow_source=true";
        return await client.GetStringAsync(url);
    }
}