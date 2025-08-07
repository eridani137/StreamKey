using System.Net.Http.Headers;
using Carter;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Serilog;
using StreamKey.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Extensions;


var builder = WebApplication.CreateBuilder(args);

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme; })
    .AddBearerToken(IdentityConstants.BearerScheme);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity().AddApiEndpoints();

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

builder.Services.AddHttpClient<ICamoufoxService, CamoufoxService>((_, client) =>
{
    client.BaseAddress = new Uri("http://camoufox:8080");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var seqEndpoint = EnvironmentHelper.GetSeqEndpoint();
var seqApiKey = EnvironmentHelper.GetSeqApiKey();
    
Log.Information("Seq endpoint: {SeqEndpoint}", seqEndpoint);
Log.Information("Seq API key: {SeqApiKey}", seqApiKey ?? "null");

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// await app.ApplyMigrations();

app.UseCors(CorsConfiguration.ProductionCorsPolicyName);

app.MapCarter();

await app.SeedDatabase();

app.Run();