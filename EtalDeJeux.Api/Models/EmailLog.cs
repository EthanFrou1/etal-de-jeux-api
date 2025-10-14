namespace EtalDeJeux.Api.Models;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string ToEmail { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? MessageId { get; set; }
    /// <summary>
    /// Expected values: "sent" | "failed".
    /// </summary>
    public string Status { get; set; } = "sent";
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order? Order { get; set; }
}
