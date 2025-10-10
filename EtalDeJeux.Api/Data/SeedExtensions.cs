using System.Collections.Generic;
using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Data;

public static class SeedExtensions
{
    public static async Task SeedAsync(this AppDbContext db)
    {
        if (db.Products.Any()) return;

        var now = DateTimeOffset.UtcNow;

        var engineBuilding = new Mechanic { Name = "Construction de moteur" };
        var handManagement = new Mechanic { Name = "Gestion de main" };
        var tilePlacement = new Mechanic { Name = "Placement de tuiles" };

        var designerHargrave = new Designer { Name = "Elizabeth Hargrave" };
        var designerFieser = new Designer { Name = "Randy Flynn" };

        var publisherStonemaier = new Publisher { Name = "Stonemaier Games", Url = "https://stonemaiergames.com" };
        var publisherAegi = new Publisher { Name = "Flatout Games", Url = "https://flatout.games" };

        var wingspan = new Product
        {
            Id = Guid.NewGuid(),
            Slug = "wingspan",
            Name = "Wingspan",
            Description = "Jeu de stratégie sur l'observation et la collection d'oiseaux.",
            Type = ProductType.BoardGame,
            Category = "Stratégie",
            Active = true,
            Images = new List<string>
            {
                "https://picsum.photos/seed/w1/800/600",
                "https://picsum.photos/seed/w2/800/600"
            },
            Languages = new List<string> { "fr", "en" },
            ReleaseYear = 2019,
            MinPlayers = 1,
            MaxPlayers = 5,
            MinAge = 10,
            PlaytimeMin = 40,
            PlaytimeMax = 70,
            Complexity = 2.4m,
            Contents = new List<ProductContentItem>
            {
                new ProductContentItem { Name = "Cartes Oiseaux", Quantity = 170 },
                new ProductContentItem { Name = "Plateaux joueurs", Quantity = 5 }
            },
            CreatedAt = now,
            UpdatedAt = now,
            Skus = new List<Sku>
            {
                new Sku
                {
                    Id = Guid.NewGuid(),
                    Name = "Boîte de base",
                    SkuCode = "WING-BASE",
                    Price = 59.99m,
                    Currency = "EUR",
                    Stock = 12,
                    Active = true,
                    IsDefault = true
                }
            },
            Files = new List<ProductFile>
            {
                new ProductFile
                {
                    Id = Guid.NewGuid(),
                    Name = "Règles FR",
                    Url = "https://example.com/wingspan/regles-fr.pdf",
                    Kind = FileKind.Rules,
                    IsPublic = true
                }
            }
        };

        wingspan.ProductMechanics.Add(new ProductMechanic { Product = wingspan, Mechanic = engineBuilding });
        wingspan.ProductMechanics.Add(new ProductMechanic { Product = wingspan, Mechanic = handManagement });
        wingspan.ProductDesigners.Add(new ProductDesigner { Product = wingspan, Designer = designerHargrave });
        wingspan.ProductPublishers.Add(new ProductPublisher { Product = wingspan, Publisher = publisherStonemaier });

        var cascadia = new Product
        {
            Id = Guid.NewGuid(),
            Slug = "cascadia",
            Name = "Cascadia",
            Description = "Puzzle de tuiles dans le nord-ouest américain.",
            Type = ProductType.BoardGame,
            Category = "Famille",
            Active = true,
            Images = new List<string> { "https://picsum.photos/seed/c1/800/600" },
            Languages = new List<string> { "fr" },
            ReleaseYear = 2021,
            MinPlayers = 1,
            MaxPlayers = 4,
            MinAge = 10,
            PlaytimeMin = 30,
            PlaytimeMax = 45,
            Complexity = 2.0m,
            Contents = new List<ProductContentItem>
            {
                new ProductContentItem { Name = "Tuiles Habitat", Quantity = 85 },
                new ProductContentItem { Name = "Jetons Faune", Quantity = 100 }
            },
            CreatedAt = now,
            UpdatedAt = now,
            Skus = new List<Sku>
            {
                new Sku
                {
                    Id = Guid.NewGuid(),
                    Name = "Édition standard",
                    SkuCode = "CASC-STD",
                    Price = 39.90m,
                    Currency = "EUR",
                    Stock = 20,
                    Active = true,
                    IsDefault = true
                }
            }
        };

        cascadia.ProductMechanics.Add(new ProductMechanic { Product = cascadia, Mechanic = tilePlacement });
        cascadia.ProductDesigners.Add(new ProductDesigner { Product = cascadia, Designer = designerFieser });
        cascadia.ProductPublishers.Add(new ProductPublisher { Product = cascadia, Publisher = publisherAegi });

        db.Products.AddRange(wingspan, cascadia);
        await db.SaveChangesAsync();
    }
}
