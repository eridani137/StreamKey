using System.ComponentModel;
using System.Diagnostics;
using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using StreamKey.Core;
using StreamKey.Core.Configuration;
using StreamKey.Core.Converters;
using StreamKey.Core.Extensions;
using StreamKey.Core.Hubs;
using StreamKey.Infrastructure.Extensions;
using StreamKey.Shared;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

if (builder.Configuration.GetSection("TelegramAuthorizationBotToken").Get<string>() is { } token)
{
    ApplicationConstants.TelegramBotToken = token;
}

builder.Services.AddHealthChecks();

builder.Services.AddSignalR()
    .AddMessagePackProtocol();

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity();

builder.Services.AddTransient<FilterNotFoundHandler>();

builder.AddDefaultAuthorization();

ConfigureCors.Configure(builder);
ConfigureJwt.Configure(builder);

builder.Services.AddHttpClients();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors(ConfigureCors.ProductionCorsPolicyName);

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();
app.MapHealthChecks("/health");

await app.SeedDatabase();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.Run();