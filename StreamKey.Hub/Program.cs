using StackExchange.Redis;
using StreamKey.Hub.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
    .AddMessagePackProtocol()
    .AddStackExchangeRedis("redis:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("StreamKey");
    });

var app = builder.Build();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.Run();