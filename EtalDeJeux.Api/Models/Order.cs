using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public string Email { get; set; } = default!;
    public string Status { get; set; } = "pending"; // pending|paid|fulfilled|canceled|refunded|partial_refund
    public string Currency { get; set; } = "EUR";

    [Column(TypeName = "numeric(12,2)")] public decimal AmountSubtotal { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal AmountDiscount { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal AmountShipping { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal AmountTax { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal AmountTotal { get; set; }

    public Guid? ReservationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Metadata { get; set; } = "{}"; // jsonb

    public List<OrderItem> Items { get; set; } = new();
}
