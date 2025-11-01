using System.ComponentModel;
using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using StreamKey.Core;
using StreamKey.Core.Configuration;
using StreamKey.Core.Converters;
using StreamKey.Core.Extensions;
using StreamKey.Infrastructure.Extensions;

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

builder.AddAdditionHeaders();

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

await app.SeedDatabase();

app.Run();