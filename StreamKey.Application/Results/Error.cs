namespace StreamKey.Application.Results;

public record Error(ErrorCode Code, string Message)
{
    public static Error None => new(ErrorCode.None, string.Empty);
    
    public static Error NullValue => new(ErrorCode.NullValue, "Значение не может быть null");
    
    public static Error StreamNotFound => new(ErrorCode.StreamNotFound, "Стрим не найден");
    public static Error PlaylistNotReceived => new(ErrorCode.PlaylistNotReceived, "Плейлист не получен");
    public static Error NotFound1080P => new(ErrorCode.NotFound1080P, "Не найден 1080p");

    public override string ToString()
    {
        return Message;
    }
} 