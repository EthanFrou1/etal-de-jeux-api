using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Sku
{
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(140)]
    public string Name { get; set; }

    [Required, MaxLength(64)]
    public string SkuCode { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal Price { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public int StockTotal { get; set; }
    public int ReservedQty { get; set; }
    public int SoldQty { get; set; }

    public bool Active { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDigital { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; }
}
