using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace EtalDeJeux.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<ProductMechanic> ProductMechanics => Set<ProductMechanic>();
    public DbSet<Designer> Designers => Set<Designer>();
    public DbSet<ProductDesigner> ProductDesigners => Set<ProductDesigner>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<ProductPublisher> ProductPublishers => Set<ProductPublisher>();
    public DbSet<ProductFile> ProductFiles => Set<ProductFile>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<WebhookEventRaw> WebhookEventsRaw => Set<WebhookEventRaw>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        var nameTranslator = new NpgsqlSnakeCaseNameTranslator();

        // --- Entities ---
        b.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();

            e.Property(p => p.Type).HasColumnType("text");
            e.Property(p => p.Images).HasColumnType("jsonb");
            e.Property(p => p.Languages).HasColumnType("text[]");
            e.Property(p => p.Contents).HasColumnType("jsonb");
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

            e.HasOne(p => p.ParentProduct)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(p => p.Skus)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Files)
                .WithOne(f => f.Product)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Sku>(e =>
        {
            e.HasIndex(s => s.SkuCode).IsUnique();
        });

        b.Entity<ProductFile>(e =>
        {
            e.Property(f => f.IsPublic).HasDefaultValue(true);
            e.Property(f => f.Kind).HasColumnType("text");
        });

        // --- Relations many-to-many ---
        b.Entity<ProductMechanic>(e =>
        {
            e.ToTable("product_mechanics");
            e.HasKey(pm => new { pm.ProductId, pm.MechanicId });
            e.Property(pm => pm.MechanicId).HasColumnName("mechanic_id");
        });

        b.Entity<ProductDesigner>(e =>
        {
            e.HasKey(pd => new { pd.ProductId, pd.DesignerId });
        });

        b.Entity<ProductPublisher>(e =>
        {
            e.HasKey(pp => new { pp.ProductId, pp.PublisherId });
        });

        b.Entity<Order>().Property(x => x.Metadata).HasColumnType("jsonb");
        b.Entity<OrderItem>().Property(x => x.Metadata).HasColumnType("jsonb");
        b.Entity<Reservation>().Property(x => x.Metadata).HasColumnType("jsonb");
        b.Entity<Payment>().Property(x => x.Metadata).HasColumnType("jsonb");
        b.Entity<PaymentEvent>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<WebhookEventRaw>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<OrderEvent>().Property(x => x.Payload).HasColumnType("jsonb");
        b.Entity<EmailLog>(e =>
        {
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Subject).HasMaxLength(200);
            e.Property(x => x.Status).HasDefaultValue("sent");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.ToEmail);

            e.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // indexes
        b.Entity<Reservation>().HasIndex(x => new { x.Status, x.ExpiresAt });
        b.Entity<Order>().HasIndex(x => new { x.Status, x.CreatedAt });
        b.Entity<Payment>().HasIndex(x => new { x.Status, x.CreatedAt });

        // unique provider+id
        b.Entity<Payment>().HasIndex(x => new { x.Provider, x.ProviderPaymentId }).IsUnique();
    }
}
