namespace StreamKey.Application.Types;

public class RateLimitInfo
{
    public int Count { get; set; }
    public DateTime ExpiresAt { get; set; }
}