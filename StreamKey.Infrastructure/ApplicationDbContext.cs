using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ChannelEntity> Channels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChannelEntity>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.OwnsOne(c => c.Info, info =>
            {
                info.Property(i => i.Title).HasMaxLength(50);
                info.Property(i => i.Thumb).HasMaxLength(200);
                info.Property(i => i.Viewers).HasMaxLength(50);
                info.Property(i => i.Description).HasMaxLength(1000);
                info.Property(i => i.Category).HasMaxLength(100);
            });
        });
    }
}