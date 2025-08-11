namespace StreamKey.Shared;

public static class ApplicationConstants
{
    public const string UsherClientName = "UsherClient";
    
    public const string LoggingPlaylists = "LoggingPlaylists";
    public const string RemoveAds = "RemoveAds";
    
    public const string PlaylistContentType = "application/vnd.apple.mpegurl";
    public const int MaxRequestsPerMinute = 100;
    public const int TimeWindowSeconds = 60;
    public static readonly Uri UsherUrl = new("https://usher.ttvnw.net");
    public const string SiteUrl = "https://www.twitch.tv";
    private const string ClientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
    
    public static Dictionary<string, string> Headers { get; } = new()
    {
        { "Accept", "*/*" },
        { "Accept-Language", "en-US" },
        { "Client-ID", ClientId },
        { "Origin", SiteUrl },
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