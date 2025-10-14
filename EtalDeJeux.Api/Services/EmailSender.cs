namespace EtalDeJeux.Api.Services;

public class EmailSender : IEmailSender
{
    public Task<(bool ok, string? messageId, string? error)> SendAsync(string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        // TODO: plug a real SMTP or SendGrid provider.
        var messageId = Guid.NewGuid().ToString("N");
        return Task.FromResult<(bool, string?, string?)>((true, messageId, null));
    }
}
