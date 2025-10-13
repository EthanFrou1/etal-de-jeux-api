using System.ComponentModel.DataAnnotations.Schema;

namespace EtalDeJeux.Api.Models;

public class Voucher
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Type { get; set; } = "gift_card"; // gift_card|product_voucher
    public string Status { get; set; } = "active";  // active|redeemed|expired|canceled
    public Guid? OrderId { get; set; }              // commande qui l'a généré
    public string IssuedToEmail { get; set; } = default!;
    [Column(TypeName = "numeric(12,2)")] public decimal? InitialAmount { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal? RemainingAmount { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Metadata { get; set; } = "{}";
}
