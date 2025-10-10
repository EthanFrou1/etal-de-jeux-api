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
    }
}
