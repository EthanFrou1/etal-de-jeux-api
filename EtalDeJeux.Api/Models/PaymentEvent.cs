namespace EtalDeJeux.Api.Models;

public class PaymentEvent
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string Type { get; set; } = default!; // intent_created|succeeded|failed|refund_created...
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
