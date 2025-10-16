using EtalDeJeux.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EtalDeJeux.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (_options.Port <= 0)
        {
            throw new InvalidOperationException("SMTP port is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("SMTP sender email is not configured.");
        }
    }

    public async Task<(bool ok, string? messageId, string? error)> SendAsync(
     string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        MimeMessage? message = null;
        try
        {
            message = BuildMessage(to, subject, body, isHtml);

            using var client = new SmtpClient
            {
                Timeout = 15000 // 15s pour éviter les hangs silencieux
            };

            var secureSocketOptions = ResolveSecureSocketOption();

            // 1) Pour tester: n'utilise pas le token d’appel HTTP.
            var sendCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var token = sendCts.Token; // remplace temporairement `ct`

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, token);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password ?? string.Empty, token);
            }

            var messageId = await client.SendAsync(message, token);
            await client.DisconnectAsync(true, token);

            var finalMessageId = string.IsNullOrWhiteSpace(messageId) ? message.MessageId : messageId;
            _logger.LogInformation("SMTP email sent to {Recipient} with Message-Id {MessageId}", to, finalMessageId);

            return (true, finalMessageId, null);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogWarning(oce, "SMTP send canceled (timeout or external token)");
            return (false, message?.MessageId, "Envoi annulé (timeout ou requête interrompue).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMTP email to {Recipient}", to);
            return (false, message?.MessageId, ex.Message);
        }
    }


    private MimeMessage BuildMessage(string to, string subject, string body, bool isHtml)
    {
        var message = new MimeMessage();

        var fromName = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : _options.FromName;

        message.From.Add(new MailboxAddress(fromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        if (isHtml)
        {
            builder.HtmlBody = body;
        }
        else
        {
            builder.TextBody = body;
        }

        message.Body = builder.ToMessageBody();

        return message;
    }

    private SecureSocketOptions ResolveSecureSocketOption()
    {
        if (_options.UseStartTls)
        {
            return SecureSocketOptions.StartTls;
        }

        if (_options.UseSsl)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        return SecureSocketOptions.Auto;
    }
}
