using System.ComponentModel.DataAnnotations;

namespace EtalDeJeux.Api.Models;

public class Customer
{
    [Key]
    public Guid IdCustomer { get; set; }

    [Required, MaxLength(100)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = default!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Adresse de livraison
    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "FR";

    // Stripe Customer ID (créé lors du premier achat)
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    // Metadata & tracking
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Relations
    public List<Order> Orders { get; set; } = new();
}