using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Data;

public static class SeedExtensions
{
    public static async Task SeedAsync(this AppDbContext db)
    {
        if (db.Products.Any()) return;

        var p1 = new Product
        {
            Id = Guid.NewGuid(),
            Slug = "wingspan",
            Name = "Wingspan",
            Description = "Jeu de stratégie sur les oiseaux.",
            Category = "Stratégie",
            Images = ["https://picsum.photos/seed/w1/800/600", "https://picsum.photos/seed/w2/800/600"],
            Skus = [
                new Sku { Id = Guid.NewGuid(), Name = "Boîte de base", PriceCents = 4999, Currency = "EUR", Stock = 12, Active = true }
            ]
        };

        var p2 = new Product
        {
            Id = Guid.NewGuid(),
            Slug = "cascadia",
            Name = "Cascadia",
            Description = "Puzzle de tuiles nature.",
            Category = "Famille",
            Images = ["https://picsum.photos/seed/c1/800/600"],
            Skus = [
                new Sku { Id = Guid.NewGuid(), Name = "Édition standard", PriceCents = 2999, Currency = "EUR", Stock = 8, Active = true }
            ]
        };

        db.Products.AddRange(p1, p2);
        await db.SaveChangesAsync();
    }
}
