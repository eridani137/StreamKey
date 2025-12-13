using System.ComponentModel;
using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using Serilog;
using StreamKey.Core;
using StreamKey.Core.Configuration;
using StreamKey.Core.Converters;
using StreamKey.Core.Extensions;
using StreamKey.Core.Observability;
using StreamKey.Infrastructure.Extensions;
using StreamKey.Shared;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.Services.PostConfigureAll<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1));

builder.Services.AddHealthChecks();

builder.AddRedis(true);

builder.AddNats(true);

builder.Services.AddApplication();

builder.Services.AddCarter();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity();

builder.Services.AddTransient<FilterNotFoundHandler>();

builder.AddDefaultAuthorizationData();

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

app.Run();