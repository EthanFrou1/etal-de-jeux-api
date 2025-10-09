using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Product
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(128)] public string Slug { get; set; } = default!;
    [Required, MaxLength(200)] public string Name { get; set; } = default!;
    public string? Description { get; set; }
    [MaxLength(64)] public string Category { get; set; } = "Divers";
    public bool Active { get; set; } = true;

    // URLs publiques (JSONB)
    public List<string> Images { get; set; } = new();

    public List<Sku> Skus { get; set; } = new();
}

public class Sku
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(128)] public string Name { get; set; } = default!;
    [MaxLength(64)] public string? SkuCode { get; set; }
    [Column(TypeName = "integer")] public int PriceCents { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "EUR";
    public int Stock { get; set; } = 0;
    public bool Active { get; set; } = true;

    // FK
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
}
