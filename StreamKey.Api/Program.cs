using System.Net.Http.Headers;
using Carter;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Serilog;
using StreamKey.Application;
using StreamKey.Application.Configuration;
using StreamKey.Application.Entities;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Services;
using StreamKey.Infrastructure;
using StreamKey.Infrastructure.Extensions;


var builder = WebApplication.CreateBuilder(args);

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    })
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddInfrastructure(builder.Configuration);

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

CorsConfiguration.ConfigureCors(builder);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapOpenApi();
app.MapScalarApiReference();

await app.ApplyMigrations();

app.UseCors(CorsConfiguration.ProductionCorsPolicyName);

app.MapCarter();

await app.SeedDatabase();

app.Run();