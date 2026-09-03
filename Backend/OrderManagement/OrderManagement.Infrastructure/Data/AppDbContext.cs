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

    public DbSet<UserStoreAccess> UserStoreAccesses
        => Set<UserStoreAccess>();

    public DbSet<Customer> Customers
        => Set<Customer>();

    public DbSet<Order> Orders
        => Set<Order>();

    public DbSet<OrderItem> OrderItems
        => Set<OrderItem>();

    public DbSet<WarehouseLocation> WarehouseLocations
        => Set<WarehouseLocation>();

    public DbSet<OrderStatusHistory> OrderStatusHistories
    => Set<OrderStatusHistory>();

    public DbSet<TrackingHistory> TrackingHistories
    => Set<TrackingHistory>();

    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<OrderTicket> OrderTickets => Set<OrderTicket>();

    // ApplicationUser is not written as a DbSet<ApplicationUser> because AppDbContext inherits from: IdentityDbContext<ApplicationUser>. It already adds the users table internally

    protected override void OnModelCreating(ModelBuilder builder) // OnModelCreating() is called automatically by EF Core
    {
        base.OnModelCreating(builder);

        ConfigureStore(builder);
        ConfigureUserStoreAccess(builder);
        ConfigureCustomer(builder);
        ConfigureOrder(builder);
        ConfigureOrderItem(builder);
        ConfigureWarehouseLocation(builder);
        ConfigureOrderStatusHistory(builder);
        ConfigureTrackingHistory(builder);
        ConfigureOrderNotes(builder);
        ConfigureOrderTicket(builder);
    }

    private static void ConfigureStore(ModelBuilder builder)
    {
        builder.Entity<Store>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ShopDomain)
                .HasMaxLength(255);

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.LastShopifyError).HasMaxLength(2000);

            entity.Property(x => x.ShopifyAccessTokenEncrypted)
                .HasMaxLength(2000);

            entity.Property(x => x.ShopifyRefreshTokenEncrypted)
                .HasMaxLength(2000);

            entity.Property(x => x.ShopifyGrantedScopes)
                .HasMaxLength(1000);

            entity.Property(x => x.ShopifyOAuthStateHash)
                .HasMaxLength(64);
        });
    }

    private static void ConfigureUserStoreAccess(ModelBuilder builder)
    {
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

    private static void ConfigureCustomer(ModelBuilder builder)
    {
        builder.Entity<Customer>(entity =>
        {
            entity.Property(x => x.ExternalCustomerId)
                .HasMaxLength(200);

            entity.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.AddressLine1)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Country)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(x => x.Store)
                .WithMany(x => x.Customers)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.StoreId);

            entity.HasIndex(x => new
            {
                x.StoreId,
                x.ExternalCustomerId
            });
        });
    }

    private static void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.Property(x => x.ExternalOrderId)
                .HasMaxLength(200);

            entity.Property(x => x.DisplayOrderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.TrackingNumber)
                .HasMaxLength(200);

            entity.Property(x => x.LocationLink)
                .HasMaxLength(1000);

            entity.Property(x => x.FinalDecision)
                .HasMaxLength(500);

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Currency)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(x => x.RowVersion)
                .IsRowVersion();

            entity.HasOne(x => x.Store)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WarehouseLocation)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.WarehouseLocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.StoreId);

            entity.HasIndex(x => x.CustomerId);

            entity.HasIndex(x => x.DisplayOrderId);

            entity.HasIndex(x => new
            {
                x.StoreId,
                x.ExternalOrderId
            })
            .IsUnique()
            .HasFilter("[ExternalOrderId] IS NOT NULL");
        });
    }

    private static void ConfigureOrderItem(ModelBuilder builder)
    {
        builder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.ExternalLineItemId)
                .HasMaxLength(200);

            entity.Property(x => x.ExternalProductId)
                .HasMaxLength(200);

            entity.Property(x => x.ExternalVariantId)
                .HasMaxLength(200);

            entity.Property(x => x.ProductName)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.VariantName)
                .HasMaxLength(200);

            entity.Property(x => x.SKU)
                .HasMaxLength(100);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.LineTotal)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.OrderId);
        });
    }

    private static void ConfigureWarehouseLocation(ModelBuilder builder)
    {
        builder.Entity<WarehouseLocation>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Country)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });
    }

    private static void ConfigureOrderStatusHistory(
    ModelBuilder builder)
    {
        builder.Entity<OrderStatusHistory>(entity =>
        {
            entity.Property(x => x.ChangedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasOne(x => x.Order)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderId);

            entity.HasIndex(x => new
            {
                x.OrderId,
                x.ChangedAtUtc
            });
        });
    }

    private static void ConfigureTrackingHistory(
    ModelBuilder builder)
    {
        builder.Entity<TrackingHistory>(entity =>
        {
            entity.Property(x => x.OldTrackingNumber)
                .HasMaxLength(200);

            entity.Property(x => x.NewTrackingNumber)
                .HasMaxLength(200);

            entity.Property(x => x.ChangedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasOne(x => x.Order)
                .WithMany(x => x.TrackingHistory)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderId);

            entity.HasIndex(x => new
            {
                x.OrderId,
                x.ChangedAtUtc
            });
        });
    }

    private static void ConfigureOrderNotes(ModelBuilder builder)
    {
        builder.Entity<OrderNote>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.NoteType)
                .IsRequired();

            entity.Property(x => x.Text)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(x => x.CreatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc)
                .IsRequired();

            entity.Property(x => x.UpdatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.OrderId,
                x.NoteType
            })
            .IsUnique();
        });
    }

    private static void ConfigureOrderTicket(ModelBuilder builder)
    {
        var entity = builder.Entity<OrderTicket>();

        entity.HasKey(x => x.Id);

        entity.Property(x => x.AssignedToUserId)
            .HasMaxLength(450)
            .IsRequired();

        entity.Property(x => x.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        entity.Property(x => x.ClosedByUserId)
            .HasMaxLength(450);

        entity.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Message)
            .HasMaxLength(4000)
            .IsRequired();

        entity.Property(x => x.Status)
            .IsRequired();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();

        entity.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.OrderId);

        entity.HasOne(x => x.Store)
    .WithMany()
    .HasForeignKey(x => x.StoreId)
    .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.StoreId);

        entity.HasIndex(x => new
        {
            x.AssignedToUserId,
            x.Status
        });
    }
}