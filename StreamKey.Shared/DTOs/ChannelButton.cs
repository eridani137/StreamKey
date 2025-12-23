namespace StreamKey.Shared.DTOs;

public record ChannelButtonDto
{
    public required string Html { get; set; }
    public required string Style { get; set; }
    public required string HoverStyle { get; set; }
    public required string ActiveStyle { get; set; }
    public required string Link { get; set; }
}