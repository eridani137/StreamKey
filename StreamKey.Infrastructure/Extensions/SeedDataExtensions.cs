using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StreamKey.Application.Interfaces;

namespace StreamKey.Infrastructure.Extensions;

public static class SeedDataExtensions
{
    public static async Task SeedDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.Seed();
    }
}