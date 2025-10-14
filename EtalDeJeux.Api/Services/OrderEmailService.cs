using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Contracts.Mapping;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Services;

public class OrderEmailService : IOrderEmailService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _sender;

    public OrderEmailService(AppDbContext db, IEmailSender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<EmailLogDto> SendOrderConfirmationAsync(Guid orderId, string? toEmailOverride, string? subjectOverride, string? bodyOverride, bool isHtml, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            throw new KeyNotFoundException("Commande introuvable");
        }

        var to = toEmailOverride ?? order.Email;
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new InvalidOperationException("Aucune adresse e-mail disponible pour cette commande");
        }

        var subject = subjectOverride ?? $"Votre commande {order.OrderNumber} — Étale de Jeux";
        var body = bodyOverride ?? BuildDefaultBody(order);

        var (ok, messageId, error) = await _sender.SendAsync(to, subject, body, isHtml, ct);

        var log = new EmailLog
        {
            OrderId = order.Id,
            ToEmail = to,
            Subject = subject,
            Body = body,
            MessageId = messageId,
            Status = ok ? "sent" : "failed",
            Error = error
        };

        _db.EmailLogs.Add(log);

        var evt = new OrderEvent
        {
            OrderId = order.Id,
            Type = "email_confirmation_sent",
            Payload = JsonSerializer.Serialize(new
            {
                to,
                subject,
                messageId,
                status = log.Status,
                error
            })
        };

        _db.OrderEvents.Add(evt);

        await _db.SaveChangesAsync(ct);

        return log.ToDto();
    }

    private static string BuildDefaultBody(Order order)
    {
        var sb = new StringBuilder();
        sb.Append("<h1>Merci pour votre commande !</h1>");
        sb.Append("<p>");
        sb.Append($"Commande <strong>{order.OrderNumber}</strong><br/>");
        sb.Append($"Passée le {order.CreatedAt:yyyy-MM-dd HH:mm} (UTC)<br/>");
        sb.Append("</p>");

        sb.Append("<table style=\"width:100%;border-collapse:collapse;\">");
        sb.Append("<thead><tr>");
        sb.Append("<th align=\"left\" style=\"border-bottom:1px solid #ccc;padding:8px;\">Article</th>");
        sb.Append("<th align=\"right\" style=\"border-bottom:1px solid #ccc;padding:8px;\">Quantité</th>");
        sb.Append("<th align=\"right\" style=\"border-bottom:1px solid #ccc;padding:8px;\">Prix unitaire</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in order.Items)
        {
            sb.Append("<tr>");
            sb.Append($"<td style=\"padding:8px;\">{item.NameSnapshot}</td>");
            sb.Append($"<td align=\"right\" style=\"padding:8px;\">{item.Qty}</td>");
            sb.Append($"<td align=\"right\" style=\"padding:8px;\">{FormatMoney(item.UnitPrice)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");

        sb.Append("<p style=\"margin-top:16px;\">");
        sb.Append($"Sous-total : {FormatMoney(order.AmountSubtotal)}<br/>");
        sb.Append($"Livraison : {FormatMoney(order.AmountShipping)}<br/>");
        sb.Append($"Taxes : {FormatMoney(order.AmountTax)}<br/>");
        sb.Append($"Total : <strong>{FormatMoney(order.AmountTotal)}</strong>");
        sb.Append("</p>");

        sb.Append("<p>Nous restons à votre disposition pour toute question.</p>");
        sb.Append("<p>L'équipe Étale de Jeux</p>");

        return sb.ToString();
    }

    private static string FormatMoney(decimal amount)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0.00} €", amount);
    }
}
