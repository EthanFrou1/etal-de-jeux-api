using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? SkuId { get; set; }
    public string NameSnapshot { get; set; } = default!;
    [Column(TypeName = "numeric(12,2)")] public decimal UnitPrice { get; set; }
    public int Qty { get; set; }
    public string? ImageUrlSnapshot { get; set; }
    public string Metadata { get; set; } = "{}";

    public Order Order { get; set; } = default!;
}
