using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using static EtalDeJeux.Api.Contracts.Dtos.CheckoutDtos;

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

        StripeConfiguration.ApiKey =
            Environment.GetEnvironmentVariable("STRIPE_SECRET") ??
            _stripeOptions.SecretKey;
    }

    public record CheckoutItem(Guid SkuId, int Qty, bool IsDigital);
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
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateSession(
          [FromBody] CreateCheckoutSessionRequest request,
          CancellationToken ct)
    {
        try
        {
            // Gérer le customer
            Models.Customer? customer = null;
            if (!string.IsNullOrEmpty(request.Email))
            {
                customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower(), ct);

                if (customer == null && !string.IsNullOrEmpty(request.FirstName))
                {
                    // Créer un nouveau customer
                    customer = new Models.Customer
                    {
                        IdCustomer = Guid.NewGuid(),
                        Email = request.Email,
                        FirstName = request.FirstName ?? "",
                        LastName = request.LastName ?? "",
                        Phone = request.Phone
                    };
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync(ct);
                }
            }

            var lineItems = new List<SessionLineItemOptions>();
            var reservationItems = new List<ReservationItem>();
            decimal totalAmount = 0;

            foreach (var item in request.Items)
            {
                var sku = await _db.Skus
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s =>
                        s.Id == item.SkuId &&
                        s.Active, ct);

                if (sku == null)
                {
                    return BadRequest(new { error = $"SKU {item.SkuId} introuvable" });
                }

                if (sku.StockTotal < item.Qty)
                {
                    return BadRequest(new
                    {
                        error = $"Stock insuffisant pour {sku.Product.Name}"
                    });
                }

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{sku.Product.Name}{(string.IsNullOrEmpty(sku.Name) ? "" : $" - {sku.Name}")}",
                            Images = sku.Product.Images?.Take(1).ToList()
                        },
                        UnitAmountDecimal = sku.Price * 100 // Stripe veut des centimes
                    },
                    Quantity = item.Qty
                });

                // Préparer les items de réservation
                reservationItems.Add(new ReservationItem
                {
                    Id = Guid.NewGuid(),
                    SkuId = sku.Id,
                    Qty = (int)item.Qty,
                    UnitPrice = sku.Price,
                    TotalPrice = sku.Price * item.Qty,
                });

                totalAmount += sku.Price * item.Qty;
            }

            // Créer la réservation
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                TotalAmount = totalAmount,
                Status = "pending",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
                Items = reservationItems
            };

            _db.Reservations.Add(reservation);

            // Décrémenter temporairement le stock
            foreach (var item in request.Items)
            {
                var sku = await _db.Skus.FindAsync(item.SkuId);
                if (sku != null)
                {
                    sku.StockTotal -= item.Qty;
                }
            }

            await _db.SaveChangesAsync(ct);

            // Créer la session Stripe
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                CustomerEmail = customer?.Email ?? request.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "reservation_id", reservation.Id.ToString() },
                    { "customer_id", customer?.IdCustomer.ToString() ?? "" }
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            // Sauvegarder l'ID de session Stripe
            reservation.StripeSessionId = session.Id;
            await _db.SaveChangesAsync(ct);

            return Ok(new CreateCheckoutSessionResponse(session.Url));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erreur Stripe");
            return BadRequest(new { error = $"Erreur Stripe: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur checkout");
            return BadRequest(new { error = "Erreur lors de la création de la session" });
        }
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
