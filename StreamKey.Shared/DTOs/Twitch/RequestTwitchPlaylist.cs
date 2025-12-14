namespace StreamKey.Shared.DTOs.Twitch;

public record RequestTwitchPlaylist
{
    public RequestTwitchPlaylistType Type { get; init; }
    public string? Username { get; init; }
    public string? VodId { get; init; }
}