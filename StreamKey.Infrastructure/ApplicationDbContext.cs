using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChannelEntity>(entity =>
        {
            entity.ToTable("Channels");
            
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.OwnsOne(c => c.Info, info =>
            {
                info.Property(i => i.Title).HasMaxLength(1000);
                info.Property(i => i.Thumb).HasMaxLength(1000);
                info.Property(i => i.Viewers).HasMaxLength(1000);
                info.Property(i => i.Description).HasMaxLength(1000);
                info.Property(i => i.Category).HasMaxLength(1000);
            });
        });

        modelBuilder.Entity<ViewStatisticEntity>(entity =>
        {
            entity.ToTable("ViewStatistics");
            
            entity.HasIndex(e => e.Id).IsUnique();
            
            entity.HasIndex(e => e.ChannelName);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<UserSessionEntity>(entity =>
        {
            entity.ToTable("UserSessions");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<ClickChannelEntity>(entity =>
        {
            entity.ToTable("ClickChannels");

            entity.HasIndex(e => e.ChannelName);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DateTime);
        });

        modelBuilder.Entity<TelegramUserEntity>(entity =>
        {
            entity.ToTable("TelegramUsers");

            entity.HasIndex(e => e.TelegramId).IsUnique();
            entity.HasIndex(e => e.IsChatMember);
            entity.HasIndex(e => e.AuthorizedAt);
        });

        modelBuilder.Entity<RestartEntity>(entity =>
        {
            entity.ToTable("Restarts");
            
            entity.HasIndex(e => e.DateTime);
        });
    }
}