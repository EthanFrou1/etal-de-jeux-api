namespace EtalDeJeux.Api.Models;

public class WebhookEventRaw
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
