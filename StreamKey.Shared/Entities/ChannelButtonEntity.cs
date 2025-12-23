namespace StreamKey.Shared.Entities;

public class ChannelButtonEntity : BaseGuidEntity
{
    public required string Html { get; set; }
    public required string Style { get; set; }
    public required string HoverStyle { get; set; }
    public required string ActiveStyle { get; set; }
    public required string Link { get; set; }
}