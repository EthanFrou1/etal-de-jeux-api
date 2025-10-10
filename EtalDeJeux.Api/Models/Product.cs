using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(120)]
    public string Slug { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    [Column(TypeName = "product_type")]
    public ProductType Type { get; set; } = ProductType.BoardGame;

    [MaxLength(80)]
    public string? Category { get; set; }

    public bool Active { get; set; } = true;

    [Column(TypeName = "jsonb")]
    public List<string> Images { get; set; } = new List<string>();

    public List<string> Languages { get; set; } = new() { "fr" };

    public short? ReleaseYear { get; set; }

    public short? MinPlayers { get; set; }

    public short? MaxPlayers { get; set; }

    public short? MinAge { get; set; }

    public short? PlaytimeMin { get; set; }

    public short? PlaytimeMax { get; set; }

    [Column(TypeName = "numeric(2,1)")]
    public decimal? Complexity { get; set; }

    public Guid? ParentProductId { get; set; }

    public Product? ParentProduct { get; set; }

    public List<Product> Children { get; set; } = new List<Product>();

    [Column(TypeName = "jsonb")]
    public List<ProductContentItem> Contents { get; set; } = new List<ProductContentItem>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<Sku> Skus { get; set; } = new List<Sku>();

    public List<ProductMechanic> ProductMechanics { get; set; } = new List<ProductMechanic>();

    public List<ProductDesigner> ProductDesigners { get; set; } = new List<ProductDesigner>();

    public List<ProductPublisher> ProductPublishers { get; set; } = new List<ProductPublisher>();

    public List<ProductFile> Files { get; set; } = new List<ProductFile>();
}
