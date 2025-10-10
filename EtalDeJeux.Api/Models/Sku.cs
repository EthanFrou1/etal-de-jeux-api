using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Sku
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(140)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(64)]
    public string SkuCode { get; set; } = default!;

    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public int? Stock { get; set; }

    public bool Active { get; set; } = true;

    public bool IsDefault { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = default!;
}
