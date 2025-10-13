namespace EtalDeJeux.Api.Models;

public class OrderEvent
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Type { get; set; } = default!; // created|paid|fulfilled|canceled|refunded|note
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
