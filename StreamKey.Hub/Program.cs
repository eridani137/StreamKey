using StackExchange.Redis;
using StreamKey.Hub.Hubs;
using StreamKey.Shared.Configs;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetSection(nameof(RedisConfig)).Get<RedisConfig>() is { } redisConfig &&
    builder.Configuration.GetSection("RedisHost").Get<string>() is { } redisHost)
{
    builder.Services.AddSignalR()
        .AddMessagePackProtocol()
        .AddStackExchangeRedis(options =>
        {
            options.Configuration = new ConfigurationOptions
            {
                EndPoints = { $"{redisHost}:{redisConfig.Port}" },
                Password = redisConfig.Password,
                ChannelPrefix = RedisChannel.Literal("StreamKey")
            };
        });
}

var app = builder.Build();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.Run();