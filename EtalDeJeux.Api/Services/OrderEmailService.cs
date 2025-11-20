using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Contracts.Mapping;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EtalDeJeux.Api.Services;

public class OrderEmailService : IOrderEmailService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _sender;
    private readonly OrderEmailOptions _orderEmailOptions;
    private readonly SmtpOptions _smtpOptions;

    public OrderEmailService(
        AppDbContext db,
        IEmailSender sender,
        IOptions<OrderEmailOptions> orderEmailOptions,
        IOptions<SmtpOptions> smtpOptions)
    {
        _db = db;
        _sender = sender;
        _orderEmailOptions = orderEmailOptions.Value;
        _smtpOptions = smtpOptions.Value;
    }

    public async Task<EmailLogDto> SendOrderConfirmationAsync(Guid orderId, SendOrderEmailDto? overrides, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            throw new KeyNotFoundException("Commande introuvable");
        }

        var payload = overrides ?? new SendOrderEmailDto();

        var to = payload.ToEmail ?? order.Email;
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new InvalidOperationException("Aucune adresse e-mail disponible pour cette commande");
        }

        var subject = payload.SubjectOverride ?? BuildCustomerSubject(order);
        var body = payload.BodyOverride ?? BuildCustomerBody(order);
        var isHtml = payload.IsHtml;

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

        var ownerEmail = payload.Owner?.ToEmail
            ?? _orderEmailOptions.OwnerEmail
            ?? _smtpOptions.FromEmail;

        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            var ownerSubject = payload.Owner?.SubjectOverride ?? BuildOwnerSubject(order);
            var ownerBody = payload.Owner?.BodyOverride ?? BuildOwnerBody(order);
            var ownerIsHtml = payload.Owner?.IsHtml ?? true;

            var (ownerOk, ownerMessageId, ownerError) = await _sender.SendAsync(ownerEmail, ownerSubject, ownerBody, ownerIsHtml, ct);

            var ownerLog = new EmailLog
            {
                OrderId = order.Id,
                ToEmail = ownerEmail,
                Subject = ownerSubject,
                Body = ownerBody,
                MessageId = ownerMessageId,
                Status = ownerOk ? "sent" : "failed",
                Error = ownerError
            };

            _db.EmailLogs.Add(ownerLog);

            var ownerEvt = new OrderEvent
            {
                OrderId = order.Id,
                Type = "email_confirmation_sent_owner",
                Payload = JsonSerializer.Serialize(new
                {
                    to = ownerEmail,
                    subject = ownerSubject,
                    messageId = ownerMessageId,
                    status = ownerLog.Status,
                    error = ownerError
                })
            };

            _db.OrderEvents.Add(ownerEvt);
        }

        await _db.SaveChangesAsync(ct);

        return log.ToDto();
    }

    private static string BuildCustomerSubject(Order order) => $"Votre commande {order.OrderNumber} — OmbreLude";

    private static string BuildOwnerSubject(Order order) => $"Nouvelle commande {order.OrderNumber} — OmbreLude";

    private static string BuildCustomerBody(Order order)
    {
        var sb = new StringBuilder();

        var hasShippingOrTax = order.AmountShipping > 0m || order.AmountTax > 0m;

        var hasCustomerInfo = !string.IsNullOrWhiteSpace(order.Email)
            || !string.IsNullOrWhiteSpace(order.CustomerFirstName)
            || !string.IsNullOrWhiteSpace(order.CustomerLastName)
            || !string.IsNullOrWhiteSpace(order.CustomerPhone);

        var fullName = string.Join(" ",
            new[] { order.CustomerFirstName, order.CustomerLastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var hasShippingAddress =
            !string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ||
            !string.IsNullOrWhiteSpace(order.ShippingAddressLine2) ||
            !string.IsNullOrWhiteSpace(order.ShippingCity) ||
            !string.IsNullOrWhiteSpace(order.ShippingPostalCode) ||
            !string.IsNullOrWhiteSpace(order.ShippingCountry);

        sb.Append(@"
            <div style=""margin:0;padding:24px;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;"">
              <div style=""max-width:600px;margin:0 auto;background-color:#ffffff;border-radius:8px;border:1px solid #e4e4e7;overflow:hidden;"">
                <!-- Header -->
                <div style=""padding:16px 24px;background-color:#111827;color:#f9fafb;border-bottom:1px solid #e5e7eb;"">
                  <h1 style=""margin:0;font-size:20px;font-weight:700;"">Merci pour votre commande !</h1>
                  <p style=""margin:4px 0 0;font-size:13px;color:#e5e7eb;"">OmbreLude</p>
                </div>

                <!-- Content -->
                <div style=""padding:24px 24px 8px 24px;font-size:14px;color:#111827;"">
        ");

        sb.Append($@"
            <p style=""margin:0 0 12px 0;line-height:1.5;"">
            Bonjour, <strong>{fullName}</strong><br/>
            Voici le récapitulatif de votre commande <strong>{order.OrderNumber}</strong> passée le 
            <strong>{order.CreatedAt:dd/MM/yyyy}</strong>.
            </p>

            <p style=""margin:0 0 16px 0;font-size:13px;color:#4b5563;"">
            Votre paiement a bien été reçu. Vous recevrez un nouvel email si des informations supplémentaires sont nécessaires.
            </p>
        ");

        // Bloc informations client
        if (hasCustomerInfo)
        {
            sb.Append(@"
                <h2 style=""margin:0 0 4px 0;font-size:15px;font-weight:600;"">Informations client</h2>
                <p style=""margin:0 0 12px 0;font-size:13px;color:#4b5563;line-height:1.5;"">
            ");

            if (!string.IsNullOrWhiteSpace(fullName))
                sb.Append($"            Nom : <strong>{fullName}</strong><br/>");

            if (!string.IsNullOrWhiteSpace(order.Email))
                sb.Append($"            Email : <strong>{order.Email}</strong><br/>");

            if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
                sb.Append($"            Téléphone : <strong>{order.CustomerPhone}</strong><br/>");

            sb.Append(@"
            </p>
            ");
        }

        // Bloc adresse de livraison (si présente)
        if (hasShippingAddress)
        {
            sb.Append(@"
                <h2 style=""margin:0 0 4px 0;font-size:15px;font-weight:600;"">Adresse de livraison</h2>
                <p style=""margin:0 0 12px 0;font-size:13px;color:#4b5563;line-height:1.5;"">
            ");

            if (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1))
                sb.Append($"{order.ShippingAddressLine1}<br/>");

            if (!string.IsNullOrWhiteSpace(order.ShippingAddressLine2))
                sb.Append($"{order.ShippingAddressLine2}<br/>");

            if (!string.IsNullOrWhiteSpace(order.ShippingPostalCode) ||
                !string.IsNullOrWhiteSpace(order.ShippingCity))
                sb.Append($"{order.ShippingPostalCode} {order.ShippingCity}<br/>");

            if (!string.IsNullOrWhiteSpace(order.ShippingCountry))
                sb.Append($"{order.ShippingCountry}<br/>");

            sb.Append(@"
            </p>
            ");
        }

        // Titre détail de commande
        sb.Append(@"
            <h2 style=""margin:0 0 8px 0;font-size:16px;font-weight:600;"">Détail de la commande</h2>
        ");

        // Tableau des lignes de commande
        sb.Append(@"
            <table style=""width:100%;border-collapse:collapse;margin-top:8px;font-size:13px;"">
            <thead>
                <tr>
                <th align=""left"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Article</th>
                <th align=""right"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Quantité</th>
                <th align=""right"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Prix unitaire</th>
                </tr>
            </thead>
            <tbody>
        ");

        foreach (var item in order.Items)
        {
            sb.Append(@"
                <tr>
            ");
            sb.Append($@"                <td style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{item.NameSnapshot}</td>");
            sb.Append($@"                <td align=""right"" style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{item.Qty}</td>");
            sb.Append($@"                <td align=""right"" style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{FormatMoney(item.UnitPrice)}</td>");
            sb.Append(@"
                </tr>
            ");
        }

        sb.Append(@"
            </tbody>
            </table>
        ");

        // Bloc récap montant
        sb.Append(@"
            <div style=""margin-top:16px;padding-top:12px;border-top:1px solid #e5e7eb;"">
            <table style=""width:100%;font-size:13px;border-collapse:collapse;"">
                <tbody>
        ");

        sb.Append($@"
                <tr>
                    <td align=""left"" style=""padding:2px 0;color:#4b5563;"">Sous-total</td>
                    <td align=""right"" style=""padding:2px 0;"">{FormatMoney(order.AmountSubtotal)}</td>
                </tr>
        ");

        if (hasShippingOrTax)
        {
            sb.Append($@"
                <tr>
                    <td align=""left"" style=""padding:2px 0;color:#4b5563;"">Livraison</td>
                    <td align=""right"" style=""padding:2px 0;"">{FormatMoney(order.AmountShipping)}</td>
                </tr>
                <tr>
                    <td align=""left"" style=""padding:2px 0;color:#4b5563;"">Taxes</td>
                    <td align=""right"" style=""padding:2px 0;"">{FormatMoney(order.AmountTax)}</td>
                </tr>
            ");
        }

        sb.Append($@"
                <tr>
                    <td align=""left"" style=""padding:6px 0;font-weight:600;font-size:14px;"">Total TTC</td>
                    <td align=""right"" style=""padding:6px 0;font-weight:700;font-size:16px;"">{FormatMoney(order.AmountTotal)}</td>
                </tr>
                </tbody>
            </table>
            </div>
        ");

        if (!hasShippingOrTax)
        {
            // Cas 100% digital
            sb.Append(@"
                <p style=""margin:12px 0 0 0;font-size:13px;color:#4b5563;"">
                Votre commande contient uniquement des produits numériques. Aucun frais de livraison ni
                taxes supplémentaires n'ont été appliqués.
                </p>
            ");
        }

        sb.Append(@"
                </div>
                <!-- Footer -->
                <div style=""padding:16px 24px;background-color:#111827;color:#f9fafb;border-top:1px solid #e5e7eb;"">
                  <p style=""margin:0 0 6px 0;font-size:13px;color:#e5e7eb;line-height:1.5;"">
                    Si vous avez la moindre question, vous pouvez répondre directement à cet email.
                  </p>
                  <p style=""margin:0;font-size:13px;color:#f9fafb;"">
                    L'équipe <strong>OmbreLude</strong>
                  </p>
                </div>
              </div>
            </div>
          </div>
        ");


        return sb.ToString();
    }

    private static string BuildOwnerBody(Order order)
    {
        var sb = new StringBuilder();

        var hasShippingAddress =
            !string.IsNullOrWhiteSpace(order.ShippingAddressLine1) ||
            !string.IsNullOrWhiteSpace(order.ShippingAddressLine2) ||
            !string.IsNullOrWhiteSpace(order.ShippingCity) ||
            !string.IsNullOrWhiteSpace(order.ShippingPostalCode) ||
            !string.IsNullOrWhiteSpace(order.ShippingCountry);

        var fullName = string.Join(" ",
            new[] { order.CustomerFirstName, order.CustomerLastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        sb.Append(@"
            <div style=""margin:0;padding:24px;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;"">
              <div style=""max-width:600px;margin:0 auto;background-color:#ffffff;border-radius:8px;border:1px solid #e4e4e7;"">
                <div style=""padding:16px 24px;border-bottom:1px solid #e5e7eb;background-color:#111827;color:#f9fafb;"">
                  <h1 style=""margin:0;font-size:19px;font-weight:700;"">Nouvelle commande reçue</h1>
                  <p style=""margin:4px 0 0;font-size:13px;color:#e5e7eb;"">OmbreLude</p>
                </div>
                <div style=""padding:24px;font-size:14px;color:#111827;"">
            ");

                sb.Append($@"
                  <p style=""margin:0 0 12px;"">
                    Commande <strong>{order.OrderNumber}</strong><br/>
                    Reçue le {order.CreatedAt:dd/MM/yyyy HH:mm} (UTC)<br/>
                    Client : <strong>{order.Email}</strong>
                  </p>
            ");

                if (!string.IsNullOrWhiteSpace(fullName) || !string.IsNullOrWhiteSpace(order.CustomerPhone))
                {
                    sb.Append(@"
                  <p style=""margin:0 0 12px;font-size:13px;color:#4b5563;line-height:1.5;"">
            ");

                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        sb.Append($"            Nom client : <strong>{fullName}</strong><br/>");
                    }

                    if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
                    {
                        sb.Append($"            Téléphone : <strong>{order.CustomerPhone}</strong><br/>");
                    }

                    sb.Append(@"
                  </p>
            ");
                }

                if (hasShippingAddress)
                {
                    sb.Append(@"
                  <h2 style=""margin:0 0 4px;font-size:15px;font-weight:600;"">Adresse de livraison</h2>
                  <p style=""margin:0 0 12px;font-size:13px;color:#4b5563;line-height:1.5;"">
            ");

                    if (!string.IsNullOrWhiteSpace(order.ShippingAddressLine1))
                        sb.Append($"{order.ShippingAddressLine1}<br/>");

                    if (!string.IsNullOrWhiteSpace(order.ShippingAddressLine2))
                        sb.Append($"{order.ShippingAddressLine2}<br/>");

                    if (!string.IsNullOrWhiteSpace(order.ShippingPostalCode) ||
                        !string.IsNullOrWhiteSpace(order.ShippingCity))
                        sb.Append($"{order.ShippingPostalCode} {order.ShippingCity}<br/>");

                    if (!string.IsNullOrWhiteSpace(order.ShippingCountry))
                        sb.Append($"{order.ShippingCountry}<br/>");

                    sb.Append(@"
                  </p>
            ");
                }

                sb.Append(@"
                  <table style=""width:100%;border-collapse:collapse;margin-top:8px;font-size:13px;"">
                    <thead>
                      <tr>
                        <th align=""left"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Article</th>
                        <th align=""right"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Quantité</th>
                        <th align=""right"" style=""padding:8px 4px;border-bottom:1px solid #e5e7eb;"">Prix unitaire</th>
                      </tr>
                    </thead>
                    <tbody>
            ");

                foreach (var item in order.Items)
                {
                    sb.Append(@"
                      <tr>
            ");
                    sb.Append($@"                <td style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{item.NameSnapshot}</td>");
                    sb.Append($@"                <td align=""right"" style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{item.Qty}</td>");
                    sb.Append($@"                <td align=""right"" style=""padding:8px 4px;border-bottom:1px solid #f4f4f5;"">{FormatMoney(item.UnitPrice)}</td>");
                    sb.Append(@"
                      </tr>
            ");
                }

                sb.Append(@"
                    </tbody>
                  </table>
                  <div style=""margin-top:16px;padding-top:12px;border-top:1px solid #e5e7eb;"">
            ");

                sb.Append($@"
                    <p style=""margin:0 0 4px;font-size:13px;color:#4b5563;"">
                      Sous-total : {FormatMoney(order.AmountSubtotal)}<br/>
                      Livraison : {FormatMoney(order.AmountShipping)}<br/>
                      Taxes : {FormatMoney(order.AmountTax)}<br/>
                      Total : <strong>{FormatMoney(order.AmountTotal)}</strong><br/>
                      Statut paiement : <strong>{order.Status}</strong>
                    </p>
            ");

                sb.Append(@"
                    <p style=""margin:12px 0 0;font-size:13px;color:#4b5563;"">
                      La commande est disponible dans le back-office pour préparation.
                    </p>
                  </div>
                </div>
              </div>
            </div>
            ");

        return sb.ToString();
    }

    private static string FormatMoney(decimal amount)
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        return string.Format(culture, "{0:C}", amount); // ex: 29,70 €
    }
}
