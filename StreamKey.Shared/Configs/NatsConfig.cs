namespace StreamKey.Shared.Configs;

public record NatsConfig
{
    public required string Url { get; set; }
    public required string Port { get; init; }
    public required string User { get; init; }
    public required string Password { get; init; }
}