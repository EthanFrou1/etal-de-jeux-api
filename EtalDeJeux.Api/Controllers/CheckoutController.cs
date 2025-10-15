using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Text;

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

        var webhookSecret =
            Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ??
            _stripeOptions.WebhookSecret;

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
                account = await service.GetAsync();
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
            account = await accountService.GetAsync();
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

        var metadata = new Dictionary<string, string>();
        if (req.OrderId.HasValue)
        {
            metadata["orderId"] = req.OrderId.Value.ToString();
        }

        if (req.ReservationId.HasValue)
        {
            metadata["reservationId"] = req.ReservationId.Value.ToString();
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            SuccessUrl = req.SuccessUrl ?? "http://localhost:5173/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = req.CancelUrl ?? "http://localhost:5173/cancel",
            CustomerEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email,
            ClientReferenceId = req.ReservationId?.ToString(),
            Metadata = metadata.Count == 0 ? null : metadata
        };

        var service = new SessionService(stripeClient);
        var session = await service.CreateAsync(options);

        _logger.LogInformation(
            "Created Stripe checkout session {SessionId} for client reference {ClientReferenceId} with URL {SessionUrl} and metadata {Metadata}",
            session.Id,
            session.ClientReferenceId ?? "(none)",
            session.Url,
            FormatMetadata(session.Metadata));

        return Ok(new { url = session.Url });
    }

    private static string FormatMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return "{}";
        }

        var builder = new StringBuilder("{");
        var first = true;
        foreach (var (key, value) in metadata)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(key);
            builder.Append('=');
            builder.Append(value);
            first = false;
        }

        builder.Append('}');
        return builder.ToString();
    }
}
