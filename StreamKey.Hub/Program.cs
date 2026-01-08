using DotNetEnv;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Hub;
using StreamKey.Shared.Hubs;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureForwardedHeaders.Configure(builder);
ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.Services.AddHostedService<InvalidateButtonsCacheListener>();

builder.Services.AddMemoryCache();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

builder.AddRedis(false);

builder.AddNats(false);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.MapHealthChecks("/health");

app.Run();