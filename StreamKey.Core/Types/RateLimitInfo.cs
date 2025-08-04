namespace StreamKey.Core.Types;

public class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsBanned { get; set; }
}