namespace StreamKey.Infrastructure.Abstractions;

public interface IDatabaseSeeder
{
    Task Seed();
}