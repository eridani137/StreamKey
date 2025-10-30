namespace StreamKey.Core.DTOs;

public record ActivityRequest(Guid SessionId, string UserId);

public record ActivityResponse(int OnlineUserCount);