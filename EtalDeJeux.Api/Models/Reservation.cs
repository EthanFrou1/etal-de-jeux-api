using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }

    [Required]
    public string Status { get; set; } = "pending"; // pending|confirmed|expired|canceled

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Metadata { get; set; } = "{}"; // jsonb

    public List<ReservationItem> Items { get; set; } = new();
}
