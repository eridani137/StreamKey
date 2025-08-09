namespace StreamKey.Core.Configs;

public record JwtConfig
{
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
}