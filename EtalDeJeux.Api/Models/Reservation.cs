using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }

    [Required]
    public string Status { get; set; } = "pending"; // pending|confirmed|expired|canceled
    public string Email { get; set; }
    public decimal TotalAmount { get; set; }
    public string? StripeSessionId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Metadata { get; set; } = "{}"; // jsonb

    public List<ReservationItem> Items { get; set; } = new();

    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
}
