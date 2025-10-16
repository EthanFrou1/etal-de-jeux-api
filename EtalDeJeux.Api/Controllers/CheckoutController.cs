using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(AppDbContext db, IOptions<StripeOptions> stripeOptions, ILogger<CheckoutController> logger)
    {
        _db = db;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    public record CheckoutItem(Guid SkuId, long Qty);
    public record CheckoutRequest(
        List<CheckoutItem> Items,
        string? Email,
        string? SuccessUrl,
        string? CancelUrl,
        Guid? ReservationId,
        Guid? OrderId);

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var secretKey =
            Environment.GetEnvironmentVariable("STRIPE_SECRET") ??
            _stripeOptions.SecretKey;

        var publishableKey =
            Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE") ??
            _stripeOptions.PublishableKey;

        var webhookSecret = _stripeOptions.WebhookSecret;

        var hasWebhookSecret = !string.IsNullOrWhiteSpace(webhookSecret);

        string? mode = null;
        if (!string.IsNullOrWhiteSpace(secretKey))
        {
            mode = secretKey.Contains("_live", StringComparison.OrdinalIgnoreCase)
                ? "live"
                : secretKey.Contains("_test", StringComparison.OrdinalIgnoreCase)
                    ? "test"
                    : null;
        }
        else if (!string.IsNullOrWhiteSpace(publishableKey))
        {
            mode = publishableKey.Contains("_live", StringComparison.OrdinalIgnoreCase)
                ? "live"
                : publishableKey.Contains("_test", StringComparison.OrdinalIgnoreCase)
                    ? "test"
                    : null;
        }

        Account? account = null;
        if (!string.IsNullOrWhiteSpace(secretKey))
        {
            try
            {
                var client = new StripeClient(secretKey);
                var service = new AccountService(client);
                account = await service.GetSelfAsync(cancellationToken: HttpContext?.RequestAborted ?? CancellationToken.None);

            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Unable to retrieve Stripe account information with the configured secret key.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error while retrieving Stripe account information.");
            }
        }

        if (account is null && string.IsNullOrWhiteSpace(publishableKey))
        {
            return NoContent();
        }

        return Ok(new { account, mode, hasWebhookSecret });
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CheckoutRequest req)
    {
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { error = "Empty items" });

        if (req.Items.Any(i => i.Qty <= 0))
            return BadRequest(new { error = "Invalid quantity" });

        var skuIds = req.Items.Select(i => i.SkuId).ToList();

        var skus = await _db.Skus.AsNoTracking()
            .Include(s => s.Product)
            .Where(s => skuIds.Contains(s.Id))
            .ToListAsync();

        if (skus.Count != skuIds.Count)
            return BadRequest(new { error = "Unknown SKU" });

        foreach (var item in req.Items)
        {
            var sku = skus.First(s => s.Id == item.SkuId);
            if (!sku.Active || !sku.Product.Active)
                return BadRequest(new { error = "One or more items are unavailable" });

            if (sku.StockTotal > 0 && sku.StockTotal < item.Qty)
                return BadRequest(new { error = "Insufficient stock" });
        }

        var stripeKey =
            Environment.GetEnvironmentVariable("STRIPE_SECRET") ??
            _stripeOptions.SecretKey;
        if (string.IsNullOrWhiteSpace(stripeKey))
        {
            return Ok(new { url = "https://checkout.stripe.com/test_session" });
        }

        var stripeClient = new StripeClient(stripeKey);
        var accountService = new AccountService(stripeClient);
        Account? account = null;
        try
        {
            account = await accountService.GetSelfAsync(cancellationToken: HttpContext?.RequestAborted ?? CancellationToken.None);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Unable to verify Stripe account before creating checkout session.");
        }

        var expectedAccountId =
            Environment.GetEnvironmentVariable("STRIPE_ACCOUNT") ??
            _stripeOptions.ExpectedAccountId;

        if (!string.IsNullOrWhiteSpace(expectedAccountId) &&
            account is not null &&
            !string.Equals(account.Id, expectedAccountId, StringComparison.Ordinal))
        {
            return Conflict(new { error = "Mauvais compte de clés Stripe" });
        }

        var reservation = await CreateReservationAsync(req, HttpContext?.RequestAborted ?? CancellationToken.None);

        var lineItems = req.Items.Select(i =>
        {
            var sku = skus.First(s => s.Id == i.SkuId);
            return new SessionLineItemOptions
            {
                Quantity = i.Qty,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)Math.Round(sku.Price * 100m, 0, MidpointRounding.AwayFromZero),
                    Currency = sku.Currency,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{sku.Product.Name} — {sku.Name}",
                        Images = sku.Product.Images?.Take(1).ToList()
                    }
                }
            };
        }).ToList();

        var metadata = new Dictionary<string, string>
        {
            ["reservationId"] = reservation.Id.ToString()
        };

        if (req.OrderId.HasValue)
        {
            metadata["orderId"] = req.OrderId.Value.ToString();
        }

        if (req.ReservationId.HasValue)
        {
            metadata["legacyReservationId"] = req.ReservationId.Value.ToString();
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            SuccessUrl = req.SuccessUrl ?? "http://localhost:8080/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = req.CancelUrl ?? "http://localhost:8080/cancel",
            CustomerEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email,
            ClientReferenceId = reservation.Id.ToString(),
            Metadata = metadata
        };

        var service = new SessionService(stripeClient);
        var session = await service.CreateAsync(options);

        _logger.LogInformation(
            "Created Stripe checkout session {SessionId} for reservation {ReservationId} with URL {SessionUrl}",
            session.Id,
            reservation.Id,
            session.Url);

        return Ok(new { url = session.Url });
    }

    private async Task<Reservation> CreateReservationAsync(CheckoutRequest req, CancellationToken ct)
    {
        if (req.ReservationId.HasValue)
        {
            var existing = await _db.Reservations
                .Include(r => r.Items)
                .SingleOrDefaultAsync(r => r.Id == req.ReservationId.Value, ct);

            if (existing is not null)
            {
                return existing;
            }

            _logger.LogWarning(
                "Requested reservation {ReservationId} was not found. A new reservation will be created.",
                req.ReservationId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Status = "pending",
            ExpiresAt = now.AddMinutes(15),
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var item in req.Items)
        {
            if (item.Qty > int.MaxValue)
            {
                throw new InvalidOperationException("Quantity exceeds supported range for reservations");
            }

            reservation.Items.Add(new ReservationItem
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                SkuId = item.SkuId,
                Qty = Convert.ToInt32(item.Qty)
            });
        }

        _db.Reservations.Add(reservation);
        _db.ReservationItems.AddRange(reservation.Items);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created reservation {ReservationId} with {ItemCount} items prior to Stripe checkout session creation.",
            reservation.Id,
            reservation.Items.Count);

        return reservation;
    }
}
