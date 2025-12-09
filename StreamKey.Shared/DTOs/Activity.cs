namespace StreamKey.Shared.DTOs;

public record ActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);