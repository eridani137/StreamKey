namespace StreamKey.Shared.Configs;

public record RedisConfig
{
    public required string Host { get; set; }
    public required string Password { get; init; }
    public required string Port { get; init; }
}