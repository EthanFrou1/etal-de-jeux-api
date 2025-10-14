namespace EtalDeJeux.Api.Services;

public interface IEmailSender
{
    Task<(bool ok, string? messageId, string? error)> SendAsync(string to, string subject, string body, bool isHtml, CancellationToken ct);
}
