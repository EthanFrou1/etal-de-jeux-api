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

        b.HasPostgresEnum<ProductType>(nameTranslator: nameTranslator);
        b.HasPostgresEnum<FileKind>(nameTranslator: nameTranslator);

        b.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();

            entity.Property(p => p.Images).HasColumnType("jsonb");
            entity.Property(p => p.Languages).HasColumnType("text[]");
            entity.Property(p => p.Contents).HasColumnType("jsonb");
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(p => p.ParentProduct)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Skus)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Files)
                .WithOne(f => f.Product)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Sku>(entity =>
        {
            entity.HasIndex(s => s.SkuCode).IsUnique();
        });

        b.Entity<ProductFile>(entity =>
        {
            entity.Property(f => f.Kind).HasDefaultValue(FileKind.Pdf);
            entity.Property(f => f.IsPublic).HasDefaultValue(true);
        });

        b.Entity<ProductMechanic>(entity =>
        {
            entity.HasKey(pm => new { pm.ProductId, pm.MechanicId });

            entity.HasOne(pm => pm.Product)
                .WithMany(p => p.ProductMechanics)
                .HasForeignKey(pm => pm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.Mechanic)
                .WithMany(m => m.ProductMechanics)
                .HasForeignKey(pm => pm.MechanicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProductDesigner>(entity =>
        {
            entity.HasKey(pd => new { pd.ProductId, pd.DesignerId });

            entity.HasOne(pd => pd.Product)
                .WithMany(p => p.ProductDesigners)
                .HasForeignKey(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pd => pd.Designer)
                .WithMany(d => d.ProductDesigners)
                .HasForeignKey(pd => pd.DesignerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProductPublisher>(entity =>
        {
            entity.HasKey(pp => new { pp.ProductId, pp.PublisherId });

            entity.HasOne(pp => pp.Product)
                .WithMany(p => p.ProductPublishers)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.Publisher)
                .WithMany(pu => pu.ProductPublishers)
                .HasForeignKey(pp => pp.PublisherId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
