namespace StreamKey.Core.Results;

public record Error(ErrorCode Code, string Message, int StatusCode)
{
    public static Error None => new(ErrorCode.None, string.Empty, 500);
    public static Error NullValue => new(ErrorCode.NullValue, "null", 500);
    public static Error StreamNotFound => new(ErrorCode.StreamNotFound, "Стрим не найден", 404);
    public static Error UnexpectedError => new(ErrorCode.UnexpectedError, "Необработанная ошибка", 500);
    public static Error PlaylistNotReceived(string detail, int statusCode) => new(ErrorCode.PlaylistNotReceived, detail, statusCode);
    public static Error Timeout => new(ErrorCode.Timeout, "Таймаут", 408);

    public static Error ChannelAlreadyExist =>
        new(ErrorCode.ChannelAlreadyExists, "Канал с таким именем уже существует", 409);
    public static Error ChannelNotFound =>
        new(ErrorCode.ChannelNotFound, "Канал не найден", 404);
    public static Error ChannelPositionIsBusy =>
        new(ErrorCode.ChannelPositionIsBusy, "Позиция уже занята", 409);

    public static Error ServerTokenNotFound =>
        new(ErrorCode.ServerTokenNotFound, "Токен не был получен", 404);
    

    public override string ToString()
    {
        return Message;
    }
}