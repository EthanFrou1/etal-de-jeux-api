using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sku> Skus => Set<Sku>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        // jsonb pour Images
        b.Entity<Product>()
            .Property(p => p.Images)
            .HasColumnType("jsonb");

        b.Entity<Product>()
            .HasMany(p => p.Skus)
            .WithOne(s => s.Product)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
