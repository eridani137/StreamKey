using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ButtonDto
{
    [Key("id")] public Guid Id { get; init; }
    [Key("html")] public required string Html { get; init; }
    [Key("style")] public required string Style { get; init; }
    [Key("hoverStyle")] public required string HoverStyle { get; init; }
    [Key("activeStyle")] public required string ActiveStyle { get; init; }
    [Key("link")] public required string Link { get; init; }
}