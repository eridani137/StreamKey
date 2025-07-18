namespace StreamKey.Application.Results;

public record Error(ErrorCode Code, string Message, int StatusCode)
{
    public static Error None => new(ErrorCode.None, string.Empty, 0);
    
    public static Error NullValue => new(ErrorCode.NullValue, "null", 0);
    
    public static Error StreamNotFound => new(ErrorCode.StreamNotFound, "Стрим не найден", 404);
    public static Error UnexpectedError => new(ErrorCode.UnexpectedError, "Необработанная ошибка", 0);
    public static Error PlaylistNotReceived(string detail, int statusCode) => new(ErrorCode.PlaylistNotReceived, detail, statusCode);
    public static Error Timeout => new(ErrorCode.Timeout, "Таймаут", 0);

    public override string ToString()
    {
        return Message;
    }
}