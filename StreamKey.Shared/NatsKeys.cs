namespace StreamKey.Shared;

public static class NatsKeys
{
    public const string Connection = "connection";
    public const string Disconnection = "disconnection";
    public const string UpdateActivity = "update.activity";
    public const string ClickChannel = "channel.click";
    public const string GetTelegramUser = "telegram.user.get";
    public const string GetChannels = "channels.get";
    public const string CheckTelegramMember = "telegram.user.check";
    public const string GetStreamBottomButtons = "buttons.stream.bottom.get";
    public const string GetLeftTopButtons = "buttons.left.top.get";
    public const string GetTopChatButtons = "buttons.top.chat.get";
    public const string ClickButton = "button.click";
    public const string InvalidateButtonsCache = "cache.buttons.invalidate";
}