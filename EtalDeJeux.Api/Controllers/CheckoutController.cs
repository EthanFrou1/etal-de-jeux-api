using EtalDeJeux.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController(AppDbContext db) : ControllerBase
{
    public record CheckoutItem(Guid SkuId, long Qty);
    public record CheckoutRequest(List<CheckoutItem> Items, string? Email, string? SuccessUrl, string? CancelUrl);

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CheckoutRequest req)
    {
        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { error = "Empty items" });

        if (req.Items.Any(i => i.Qty <= 0))
            return BadRequest(new { error = "Invalid quantity" });

        var skuIds = req.Items.Select(i => i.SkuId).ToList();

        var skus = await db.Skus.AsNoTracking()
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

            if (sku.Stock.HasValue && sku.Stock.Value < item.Qty)
                return BadRequest(new { error = "Insufficient stock" });
        }

        var stripeKey = Environment.GetEnvironmentVariable("STRIPE_SECRET");
        if (string.IsNullOrWhiteSpace(stripeKey))
        {
            return Ok(new { url = "https://checkout.stripe.com/test_session" });
        }

        StripeConfiguration.ApiKey = stripeKey;

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

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            SuccessUrl = req.SuccessUrl ?? "http://localhost:5173/success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = req.CancelUrl ?? "http://localhost:5173/cancel",
            CustomerEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new { url = session.Url });
    }
}
