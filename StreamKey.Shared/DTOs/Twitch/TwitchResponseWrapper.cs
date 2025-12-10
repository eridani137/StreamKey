namespace StreamKey.Shared.DTOs.Twitch;

public record TwitchResponseWrapper<T>(
    T? Data,
    string RawJson
);