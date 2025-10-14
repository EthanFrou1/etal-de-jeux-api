namespace EtalDeJeux.Api.Contracts.Dtos;

public record SendOrderEmailDto(string? ToEmail, string? SubjectOverride, string? BodyOverride, bool IsHtml = true);

public record EmailLogDto(Guid Id, Guid? OrderId, string ToEmail, string Subject, string? MessageId, string Status, string? Error, DateTimeOffset CreatedAt);

public record PaginatedEmailLogsDto(IReadOnlyList<EmailLogDto> Items, int Total, int Page);
