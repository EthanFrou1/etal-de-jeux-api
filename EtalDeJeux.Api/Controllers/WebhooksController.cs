using System;
using System.Text;
using System.Text.Json;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using EtalDeJeux.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderEmailService _orderEmailService;
    private readonly IOptions<StripeOptions> _stripeOptions;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        AppDbContext db,
        IOrderEmailService orderEmailService,
        IOptions<StripeOptions> stripeOptions,
        ILogger<WebhooksController> logger)
    {
        _db = db;
        _orderEmailService = orderEmailService;
        _stripeOptions = stripeOptions;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        string payload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync();
        }

        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        string? eventId = null;
        string? eventType = null;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                eventId = idElement.GetString();
            }

            if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
            {
                eventType = typeElement.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Impossible de parser le payload Stripe pour extraire les métadonnées de journalisation");
        }

        var secret = _stripeOptions.Value.WebhookSecret;
        var hasSecret = !string.IsNullOrWhiteSpace(secret);

        _logger.LogInformation(
            "Réception de l'événement Stripe {EventType} ({EventId}). Clé secrète configurée : {HasSecret}",
            eventType ?? "(inconnu)",
            eventId ?? "(inconnu)",
            hasSecret);

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            _logger.LogWarning("Webhook Stripe reçu sans signature");
            return BadRequest();
        }

        if (!hasSecret)
        {
            _logger.LogError("Webhook Stripe reçu mais aucune clé secrète configurée");
            return StatusCode(StatusCodes.Status500InternalServerError, "Stripe webhook secret not configured");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                secret,
                throwOnApiVersionMismatch: false
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Signature Stripe invalide");
            return BadRequest();
        }

        eventId = stripeEvent.Id;
        var type = stripeEvent.Type ?? string.Empty;

        if (string.IsNullOrWhiteSpace(eventId))
        {
            _logger.LogWarning("Événement Stripe sans identifiant");
            return BadRequest();
        }

        var exists = await _db.WebhookEventsRaw.AnyAsync(w => w.EventId == eventId, ct);
        if (exists)
        {
            _logger.LogInformation("Événement Stripe {EventId} déjà traité", eventId);
            return Ok();
        }

        _db.WebhookEventsRaw.Add(new WebhookEventRaw
        {
            EventId = eventId,
            Type = type,
            Payload = payload
        });

        Order? orderToEmail = null;

        if (type == EventTypes.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;

            if (session is null && stripeEvent.Data.Object is StripeEntity entity && entity.RawJObject is { } raw)
            {
                var parsedSession = Session.FromJson(raw.ToString());
                if (parsedSession is not null)
                {
                    session = parsedSession;
                }
            }

            if (session is null)
            {
                _logger.LogWarning("Impossible de convertir l'événement {EventId} en session Stripe", eventId);
            }
            else
            {
                try
                {
                    orderToEmail = await HandleCheckoutSessionCompletedAsync(session, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du traitement de la session Stripe {SessionId}", session.Id);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        if (orderToEmail is not null)
        {
            try
            {
                await _orderEmailService.SendOrderConfirmationAsync(orderToEmail.Id, null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de commande {OrderId}", orderToEmail.Id);
            }
        }

        return Ok();
    }

    private async Task<Order?> HandleCheckoutSessionCompletedAsync(Session session, CancellationToken ct)
    {
        var metadataReservationIdCamel = GetMetadataValue(session, "reservationId");
        var metadataReservationIdSnake = GetMetadataValue(session, "reservation_id");
        var metadataOrderIdCamel = GetMetadataValue(session, "orderId");
        var metadataOrderIdSnake = GetMetadataValue(session, "order_id");

        _logger.LogInformation(
            "Traitement de la session Stripe {SessionId} (Email: {CustomerEmail}, ClientReferenceId: {ClientReferenceId}, MetadataReservationId: {MetadataReservationId}, MetadataOrderId: {MetadataOrderId}, Statut de paiement: {PaymentStatus})",
            session.Id,
            session.CustomerEmail ?? "(aucun)",
            session.ClientReferenceId ?? "(aucun)",
            metadataReservationIdCamel ?? metadataReservationIdSnake ?? "(aucun)",
            metadataOrderIdCamel ?? metadataOrderIdSnake ?? "(aucun)",
            session.PaymentStatus ?? "(inconnu)");

        var reservationId = TryGetGuid(session.ClientReferenceId)
            ?? TryGetGuid(metadataReservationIdSnake)
            ?? TryGetGuid(metadataReservationIdCamel);

        var orderId = TryGetGuid(metadataOrderIdSnake)
            ?? TryGetGuid(metadataOrderIdCamel);

        Order? order = null;
        if (orderId.HasValue)
        {
            order = await _db.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId.Value, ct);
        }

        if (order is null && reservationId.HasValue)
        {
            order = await _db.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.ReservationId == reservationId.Value, ct);
        }

        Reservation? reservation = null;
        if (reservationId.HasValue)
        {
            reservation = await _db.Reservations
                .Include(r => r.Items)
                .ThenInclude(i => i.Sku)
                .ThenInclude(s => s.Product)
                .SingleOrDefaultAsync(r => r.Id == reservationId.Value, ct);
        }
        else if (order?.ReservationId is Guid rid)
        {
            reservation = await _db.Reservations
                .Include(r => r.Items)
                .ThenInclude(i => i.Sku)
                .ThenInclude(s => s.Product)
                .SingleOrDefaultAsync(r => r.Id == rid, ct);
        }

        _logger.LogInformation(
            "Session {SessionId} associée à OrderId={OrderId} ReservationId={ReservationId}",
            session.Id,
            order?.Id ?? orderId,
            reservation?.Id ?? reservationId);

        if (order is null)
        {
            if (reservation is null)
            {
                _logger.LogWarning("Impossible de trouver une réservation associée à la session {SessionId}", session.Id);
                return null;
            }

            var generatedOrderId = orderId ?? Guid.NewGuid();
            order = new Order
            {
                Id = generatedOrderId,
                ReservationId = reservation.Id,
                OrderNumber = GetOrCreateOrderNumber(session),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Orders.Add(order);

            _logger.LogInformation(
                "Création d'une nouvelle commande {OrderId} à partir de la réservation {ReservationId} pour la session {SessionId}",
                order.Id,
                reservation.Id,
                session.Id);
        }
        else
        {
            await _db.Entry(order).Collection(o => o.Items).LoadAsync(ct);
            if (string.IsNullOrWhiteSpace(order.OrderNumber))
            {
                order.OrderNumber = GetOrCreateOrderNumber(session);
            }
        }

        var currency = (session.Currency ?? order.Currency)?.ToUpperInvariant() ?? "EUR";

        order.Email = session.CustomerDetails?.Email
            ?? session.CustomerEmail
            ?? order.Email;

        var previousOrderStatus = order.Status;
        order.Status = "paid";
        order.Currency = currency;
        order.AmountSubtotal = ConvertAmount(session.AmountSubtotal);
        order.AmountDiscount = ConvertAmount(session.TotalDetails?.AmountDiscount);
        order.AmountShipping = ConvertAmount(session.TotalDetails?.AmountShipping);
        order.AmountTax = ConvertAmount(session.TotalDetails?.AmountTax);
        order.AmountTotal = ConvertAmount(session.AmountTotal);
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Commande {OrderId} : statut {OldStatus} -> {NewStatus}",
            order.Id,
            string.IsNullOrWhiteSpace(previousOrderStatus) ? "(aucun)" : previousOrderStatus,
            order.Status);

        if (reservation is not null)
        {
            order.ReservationId = reservation.Id;
            var previousReservationStatus = reservation.Status;
            reservation.Status = "confirmed";
            reservation.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.Equals(previousReservationStatus, reservation.Status, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Réservation {ReservationId} : statut {OldStatus} -> {NewStatus}",
                    reservation.Id,
                    string.IsNullOrWhiteSpace(previousReservationStatus) ? "(aucun)" : previousReservationStatus,
                    reservation.Status);
            }

            // 🔹 NEW : snapshot adresse de livraison depuis Reservation
            order.ShippingAddressLine1 ??= reservation.ShippingAddressLine1;
            order.ShippingAddressLine2 ??= reservation.ShippingAddressLine2;
            order.ShippingCity ??= reservation.ShippingCity;
            order.ShippingPostalCode ??= reservation.ShippingPostalCode;
            order.ShippingCountry ??= reservation.ShippingCountry;

            // 🔹 NEW : snapshot client depuis Customer (via reservation.CustomerId)
            if (reservation.CustomerId.HasValue)
            {
                if (!order.CustomerId.HasValue)
                {
                    order.CustomerId = reservation.CustomerId;
                }

                var customer = await _db.Customers
                    .SingleOrDefaultAsync(c => c.Id == reservation.CustomerId.Value, ct);

                if (customer is not null)
                {
                    order.CustomerFirstName ??= customer.FirstName;
                    order.CustomerLastName ??= customer.LastName;
                    order.CustomerPhone ??= customer.Phone;

                    // fallback email si jamais on n'en a toujours pas
                    if (string.IsNullOrWhiteSpace(order.Email))
                    {
                        order.Email = customer.Email;
                    }
                }
            }
        }

        if (order.Items.Count == 0 && reservation?.Items is { Count: > 0 })
        {
            foreach (var item in reservation.Items)
            {

                var productName = item.Sku?.Product?.Name;
                var skuPrice = item.Sku?.Price ?? 0m;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    SkuId = item.SkuId,
                    Qty = item.Qty,
                    NameSnapshot = productName,
                    UnitPrice = skuPrice
                });

                if (item.Sku is not null)
                {
                    item.Sku.ReservedQty = Math.Max(0, item.Sku.ReservedQty - item.Qty);
                    item.Sku.SoldQty += item.Qty;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(session.PaymentIntentId))
        {
            var payment = await _db.Payments
                .SingleOrDefaultAsync(p => p.Provider == "stripe" && p.ProviderPaymentId == session.PaymentIntentId, ct);

            if (payment is null)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    Provider = "stripe",
                    ProviderPaymentId = session.PaymentIntentId,
                    OrderId = order.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.Payments.Add(payment);
            }

            payment.OrderId = order.Id;
            payment.Status = MapPaymentStatus(session.PaymentStatus);
            payment.Amount = ConvertAmount(session.AmountTotal);
            payment.Currency = currency;
            payment.CapturedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            payment.Metadata = JsonSerializer.Serialize(new
            {
                sessionId = session.Id,
                paymentStatus = session.PaymentStatus
            });
        }

        if (string.IsNullOrWhiteSpace(order.Email))
        {
            _logger.LogWarning("Commande {OrderId} sans e-mail — confirmation non envoyée", order.Id);
            return null;
        }

        _logger.LogInformation(
            "Session {SessionId}: email client {CustomerEmail} traité avec statut de paiement {PaymentStatus} pour la commande {OrderId}",
            session.Id,
            order.Email,
            session.PaymentStatus ?? "(inconnu)",
            order.Id);

        return order;
    }

    private static Guid? TryGetGuid(string? value)
    {
        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string? GetMetadataValue(Session session, string key)
    {
        return session.Metadata != null && session.Metadata.TryGetValue(key, out var val) ? val : null;
    }

    private static decimal ConvertAmount(long? amount)
    {
        return amount.HasValue ? amount.Value / 100m : 0m;
    }

    private static string MapPaymentStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "paid" => "succeeded",
            "no_payment_required" => "succeeded",
            "unpaid" => "pending",
            "requires_payment_method" => "requires_action",
            _ => "pending"
        };
    }

    private static string GetOrCreateOrderNumber(Session session)
    {
        if (session.Metadata != null)
        {
            if (session.Metadata.TryGetValue("order_number", out var number) && !string.IsNullOrWhiteSpace(number))
            {
                return number;
            }
        }

        return $"OJ-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
    }
}
