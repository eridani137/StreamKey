using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Infrastructure.Repositories.Cached;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Database"));
            });

            services.AddScoped<ChannelRepository>();
            services.AddScoped<IChannelRepository, CachedChannelRepository>();
            
            services.AddScoped<ButtonRepository>();
            services.AddScoped<IButtonRepository, CachedButtonRepository>();

            services.AddScoped<ViewStatisticRepository>();
            services.AddScoped<UserSessionRepository>();
            services.AddScoped<ChannelClickRepository>();
            services.AddScoped<ButtonClickRepository>();

            services.AddMemoryCache();

            services.AddScoped<TelegramUserRepository>();
            services.AddScoped<ITelegramUserRepository, CachedTelegramUserRepository>();
            
            services.AddScoped<IRestartRepository, RestartRepository>();
        
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        public IdentityBuilder AddIdentity()
        {
            return services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequiredUniqueChars = 3;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();
        }
    }
}
