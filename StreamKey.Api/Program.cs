using System.Net.Http.Headers;
using Carter;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using StreamKey.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Extensions;


var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity();//.AddApiEndpoints();

builder.Services.AddHttpClient<IUsherService, UsherService>((_, client) =>
    {
        client.BaseAddress = StaticData.UsherUrl;
        client.DefaultRequestHeaders.Referrer = new Uri(StaticData.SiteUrl);
        foreach (var header in StaticData.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
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

// await app.ApplyMigrations();

app.UseCors(ConfigureCors.ProductionCorsPolicyName);

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

await app.SeedDatabase();

app.Run();