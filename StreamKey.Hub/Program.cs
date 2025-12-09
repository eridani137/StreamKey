using DotNetEnv;
using Serilog.Events;
using StackExchange.Redis;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Hub.Hubs;
using StreamKey.Shared.Configs;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder, LogEventLevel.Debug);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.Services.AddHealthChecks();

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

app.MapHealthChecks("/health");

app.Run();