using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Core.Entities;

namespace OrderManagement.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<UserStoreAccess> UserStoreAccesses => Set<UserStoreAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Store>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });

        builder.Entity<UserStoreAccess>(entity =>
        {
            entity.HasKey(x => new
            {
                x.UserId,
                x.StoreId
            });

            entity.HasOne(x => x.User)
                .WithMany(x => x.StoreAccesses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Store)
                .WithMany(x => x.UserAccesses)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}