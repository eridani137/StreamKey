using System.ComponentModel;
using System.Diagnostics;
using Carter;
using DotNetEnv;
using Microsoft.Extensions.FileProviders;
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

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity();

builder.Services.AddTransient<FilterNotFoundHandler>();

builder.AddAdditionHeaders();

ConfigureCors.Configure(builder);
ConfigureJwt.Configure(builder);

builder.Services.AddHttpClients();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/openapi/v1.json"))
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }

    await next();
});

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post)
    {
        context.Request.EnableBuffering();
        
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var activity = Activity.Current;
        activity?.SetTag("http.request.body", body);
    }

    await next();
});

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors(ConfigureCors.ProductionCorsPolicyName);

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

await app.SeedDatabase();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.Run();