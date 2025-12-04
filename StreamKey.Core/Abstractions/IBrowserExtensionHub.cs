using StreamKey.Core.DTOs;

namespace StreamKey.Core.Abstractions;

public interface IBrowserExtensionHub
{
    Task RequestUserData();

    Task ReloadUserData(TelegramUserDto user);

    // Task Abort();
}