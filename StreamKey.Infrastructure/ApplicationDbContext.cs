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
    }
}