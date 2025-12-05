namespace StreamKey.Core.DTOs.TwitchGraphQL;

public record TwitchResponseWrapper<T>(
    T? Data,
    string RawJson
);