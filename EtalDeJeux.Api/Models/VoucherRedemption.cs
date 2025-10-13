using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class VoucherRedemption
{
    public Guid Id { get; set; }
    public Guid VoucherId { get; set; }
    public Guid RedeemedByOrderId { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal AmountUsed { get; set; } = 0m;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Metadata { get; set; } = "{}";
}
