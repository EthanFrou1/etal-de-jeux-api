namespace EtalDeJeux.Api.Contracts.Dtos;

public record SendOrderEmailDto
{
    public string? ToEmail { get; init; }

    public string? SubjectOverride { get; init; }

    public string? BodyOverride { get; init; }

    public bool IsHtml { get; init; } = true;

    public OrderEmailRecipientDto? Owner { get; init; }
}

public record OrderEmailRecipientDto
{
    public string? ToEmail { get; init; }

    public string? SubjectOverride { get; init; }

    public string? BodyOverride { get; init; }

    public bool? IsHtml { get; init; }
}

public record EmailLogDto(Guid Id, Guid? OrderId, string ToEmail, string Subject, string? MessageId, string Status, string? Error, DateTimeOffset CreatedAt);

public record PaginatedEmailLogsDto(IReadOnlyList<EmailLogDto> Items, int Total, int Page);

public class ContactMessageDto
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
}
