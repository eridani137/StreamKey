namespace StreamKey.Shared.Entities;

public class ButtonEntity : BaseGuidEntity
{
    public required string Html { get; set; }
    public required string Style { get; set; }
    public required string HoverStyle { get; set; }
    public required string ActiveStyle { get; set; }
    public required string Link { get; set; }
    public required bool IsEnabled { get; set; }
}