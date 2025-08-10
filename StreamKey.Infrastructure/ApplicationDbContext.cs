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
            
            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });
    }
}