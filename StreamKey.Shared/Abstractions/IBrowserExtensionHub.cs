using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Shared.Abstractions;

public interface IBrowserExtensionHub
{
    Task RequestUserData();

    Task ReloadUserData(TelegramUserDto user);
}