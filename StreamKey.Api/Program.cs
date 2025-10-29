using System.Net.Http.Headers;
using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using StreamKey.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Extensions;
using StreamKey.Shared;

var uploads = Path.Combine(Directory.GetCurrentDirectory(), "files");
Directory.CreateDirectory(uploads);

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity();

builder.Services.AddTransient<FilterNotFoundHandler>();

var authorization = builder.Configuration.GetSection("Authorization");
if (authorization.Exists() && !string.IsNullOrEmpty(authorization.Value))
{
    ApplicationConstants.Headers.Add("Authorization", authorization.Value);
}

var deviceId = builder.Configuration.GetSection("DeviceId");
if (deviceId.Exists() && !string.IsNullOrEmpty(deviceId.Value))
{
    ApplicationConstants.Headers.Add("x-device-id", deviceId.Value);
}

builder.Services.AddHttpClient(ApplicationConstants.UsherClientName, (_, client) =>
    {
        client.BaseAddress = ApplicationConstants.UsherUrl;
        client.DefaultRequestHeaders.Referrer = new Uri(ApplicationConstants.TwitchUrl);

        foreach (var header in ApplicationConstants.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<FilterNotFoundHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient(ApplicationConstants.ServerClientName, (_, client) =>
    {
        client.BaseAddress = ApplicationConstants.QqlUrl;
        client.DefaultRequestHeaders.Referrer = new Uri(ApplicationConstants.TwitchUrl);

        foreach (var header in ApplicationConstants.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<FilterNotFoundHandler>()
    .AddStandardResilienceHandler();

ConfigureCors.Configure(builder);
ConfigureJwt.Configure(builder);

builder.Services.AddHttpClient<ICamoufoxService, CamoufoxService>((_, client) =>
{
    client.BaseAddress = new Uri("http://camoufox:8080");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors(ConfigureCors.ProductionCorsPolicyName);

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

await app.SeedDatabase();

app.Run();