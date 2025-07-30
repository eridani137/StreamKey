using System.Net.Http.Headers;
using Carter;
using Scalar.AspNetCore;
using Serilog;
using StreamKey.Application;
using StreamKey.Application.Configuration;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Services;


var builder = WebApplication.CreateBuilder(args);

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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

app.UseCors(CorsConfiguration.ProductionCorsPolicyName);

app.MapCarter();

app.Run();