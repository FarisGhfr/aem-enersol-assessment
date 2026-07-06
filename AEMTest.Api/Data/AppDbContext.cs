using AEMTest.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AEMTest.Api.Data;

/// <summary>
/// Entity Framework Core DbContext for the AEM Test application.
/// Uses Code First approach — schema is managed via EF migrations.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<Well> Wells => Set<Well>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Platform configuration
        modelBuilder.Entity<Platform>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever(); // Use API-provided IDs

            entity.Property(p => p.UniqueName).HasMaxLength(500);

            entity.HasMany(p => p.Wells)
                  .WithOne(w => w.Platform)
                  .HasForeignKey(w => w.PlatformId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Well configuration
        modelBuilder.Entity<Well>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).ValueGeneratedNever(); // Use API-provided IDs

            entity.Property(w => w.UniqueName).HasMaxLength(500);
        });
    }
}
