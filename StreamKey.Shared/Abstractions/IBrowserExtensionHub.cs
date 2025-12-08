using StreamKey.Shared.DTOs;

namespace StreamKey.Shared.Abstractions;

public interface IBrowserExtensionHub
{
    Task RequestUserData();

    Task ReloadUserData(TelegramUserDto user);
}