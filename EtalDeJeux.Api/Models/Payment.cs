using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = "stripe";
    public string ProviderPaymentId { get; set; } = default!;
    public string Status { get; set; } = "pending"; // requires_action|pending|succeeded|failed|refunded|partial_refund
    [Column(TypeName = "numeric(12,2)")] public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTimeOffset? CapturedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Metadata { get; set; } = "{}";

    public Order Order { get; set; } = default!;
}
