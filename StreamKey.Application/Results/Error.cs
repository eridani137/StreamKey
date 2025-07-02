namespace StreamKey.Application.Results;

public record Error(ErrorCode Code, string Message)
{
    public static Error None => new(ErrorCode.None, string.Empty);
    
    public static Error NullValue => new(ErrorCode.NullValue, "null");
    
    public static Error StreamNotFound => new(ErrorCode.StreamNotFound, "Стрим не найден");
    public static Error PlaylistNotReceived => new(ErrorCode.PlaylistNotReceived, "Плейлист не получен");
    public static Error UnexpectedError => new(ErrorCode.UnexpectedError, "Необработанная ошибка");
    public static Error Timeout => new(ErrorCode.Timeout, "Таймаут");

    public override string ToString()
    {
        return Message;
    }
} 