namespace StreamKey.Shared;

public static class RedisKeys
{
    // Набор всех активных соединений
    public const string ActiveConnectionsSet = "sr:conn:active:set";

    // Базовый префикс активных соединений
    public const string ActiveConnectionPrefix = "sr:conn:active";

    // Базовый префикс отключённых соединений
    public const string DisconnectedConnectionPrefix = "sr:conn:disc";

    // Индекс SessionId → ConnectionId
    public const string SessionIndexPrefix = "sr:session";


    public static string ActiveConnection(string connectionId)
        => $"{ActiveConnectionPrefix}:{connectionId}";

    public static string DisconnectedConnection(string connectionId)
        => $"{DisconnectedConnectionPrefix}:{connectionId}";

    public static string SessionIndex(Guid sessionId)
        => $"{SessionIndexPrefix}:{sessionId}";
}