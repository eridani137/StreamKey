namespace StreamKey.Shared;

public static class ApplicationConstants
{
    public const string UsherClientName = "UsherClient";
    public const string ServerClientName = "ServerClient";
    
    public const string PlaylistContentType = "application/vnd.apple.mpegurl";
    
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");
    public static readonly Uri TwitchUrl = new("https://www.twitch.tv");
    public static readonly Uri QqlUrl = new("https://gql.twitch.tv/gql");
    
    public const string ClientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
    
    public static string TelegramBotToken { get; set; } = string.Empty;
    public static readonly Uri TelegramUrl = new ("https://api.telegram.org");
    public const string TelegramClientName = "TelegramClient";
    public const long TelegramChatId = -1001578482756;
    
    public static string? DefaultAuthorization { get; set; }
    public static string? DefaultDeviceId { get; set; }
    
    public static Dictionary<string, string> Headers { get; } = new()
    {
        { "Accept", "*/*" },
        { "Accept-Language", "en-US" },
        { "client-id", ClientId },
        { "Origin", TwitchUrl.AbsoluteUri },
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
    };
}