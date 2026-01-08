namespace StreamKey.Shared;

public static class ApplicationConstants
{
    public const string UsherClientName = "UsherClient";
    public const string TwitchClientName = "TwitchClient";
    
    public const string PlaylistContentType = "application/vnd.apple.mpegurl";
    
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");
    public static readonly Uri TwitchUrl = new("https://www.twitch.tv");
    public static readonly Uri GqlUrl = new("https://gql.twitch.tv/gql");
    
    public const string ClientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
    
    public static readonly Uri TelegramUrl = new ("https://api.telegram.org");
    public const string TelegramClientName = "TelegramClient";
    public const long TelegramChatId = -1001578482756;
    
    public static string? DefaultAuthorization { get; set; }
    public static string? DefaultDeviceId { get; set; }
    
    public static Dictionary<string, string> Headers { get; } = new()
    {
        { "Origin", TwitchUrl.AbsoluteUri },
        { "Client-ID", ClientId },
    };
}