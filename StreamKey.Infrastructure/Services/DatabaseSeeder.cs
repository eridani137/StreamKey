using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Services;

public class DatabaseSeeder(
    UserManager<ApplicationUser> userManager,
    ILogger<DatabaseSeeder> logger,
    ISettingsRepository settingsRepository
) : IDatabaseSeeder
{
    public async Task Seed()
    {
        const string rootUsername = "root";
        const string rootPassword = "Qwerty123_";

        var rootUser = await userManager.FindByNameAsync(rootUsername);
        if (rootUser is null)
        {
            rootUser = new ApplicationUser()
            {
                UserName = rootUsername
            };
            
            var result = await userManager.CreateAsync(rootUser, rootPassword);
            if (!result.Succeeded)
            {
                logger.LogError(string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
            
            logger.LogInformation($"root пользователь успешно создан");
        }

        if (await settingsRepository.GetValue<BaseSettings>(nameof(BaseSettings)) is null)
        {
            await settingsRepository.SetValue(nameof(BaseSettings), new BaseSettings());
        }
    }
}