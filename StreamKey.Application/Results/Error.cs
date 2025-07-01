namespace StreamKey.Application.Results;

public record Error(ErrorCode Code, string Message)
{
    public static Error None => new(ErrorCode.None, string.Empty);
    
    public static Error NullValue => new(ErrorCode.NullValue, "Значение не может быть null");
    
    public static Error StreamNotFound => new(ErrorCode.StreamNotFound, "Стрим не найден");

    public override string ToString()
    {
        return Message;
    }
} 