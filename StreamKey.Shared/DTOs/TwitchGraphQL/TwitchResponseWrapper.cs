namespace StreamKey.Shared.DTOs.TwitchGraphQL;

public record TwitchResponseWrapper<T>(
    T? Data,
    string RawJson
);