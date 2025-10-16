using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EtalDeJeux.Api.Options;
using Microsoft.Extensions.Options;
using System.Linq;

namespace EtalDeJeux.Api.Services;

public class EmailSender : IEmailSender
{
    private static readonly Uri EmailJsEndpoint = new("https://api.emailjs.com/api/v1.0/email/send");

    private readonly HttpClient _httpClient;
    private readonly EmailJsOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(HttpClient httpClient, IOptions<EmailJsOptions> options, ILogger<EmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ServiceId) ||
            string.IsNullOrWhiteSpace(_options.TemplateId) ||
            string.IsNullOrWhiteSpace(_options.PublicKey) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException("EmailJS options are not fully configured.");
        }
    }

    public async Task<(bool ok, string? messageId, string? error)> SendAsync(
     string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EmailJsEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PrivateKey);

            var payload = new
            {
                service_id = _options.ServiceId,
                template_id = _options.TemplateId,
                user_id = _options.PublicKey, // <-- requis
                template_params = new Dictionary<string, object?>
                {
                    ["subject"] = subject,
                    ["to_email"] = to,
                    ["from_email"] = _options.FromEmail,
                    ["from_name"] = _options.FromName,
                    ["reply_to"] = _options.FromEmail,
                    [isHtml ? "message_html" : "message"] = body
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return (true, ExtractMessageId(response, content), null);

            return (false, null, string.IsNullOrWhiteSpace(content) ? $"{(int)response.StatusCode} {response.ReasonPhrase}" : content);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, null, ex.Message);
        }
    }

    private static string? ExtractMessageId(HttpResponseMessage response, string content)
    {
        if (response.Headers.TryGetValues("X-Request-Id", out var requestIds))
        {
            return requestIds.FirstOrDefault();
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore parsing issues for message id extraction.
        }

        return null;
    }
}
