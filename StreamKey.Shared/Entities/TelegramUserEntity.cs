namespace StreamKey.Shared.Entities;

public class TelegramUserEntity : BaseIntEntity
{
    public long TelegramId { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;
    
    public long AuthDate { get; set; }
    
    public string PhotoUrl { get; set; } = string.Empty;
    
    public required string Hash { get; set; }
    
    public bool IsChatMember { get; set; }
    
    public DateTime AuthorizedAt { get; set; }
}