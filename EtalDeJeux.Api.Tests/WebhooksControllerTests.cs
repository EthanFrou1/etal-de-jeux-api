using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Controllers;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using EtalDeJeux.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EtalDeJeux.Api.Tests;

public class WebhooksControllerTests
{
    [Fact]
    public async Task Stripe_CheckoutSessionCompleted_PersistsOrderAndSendsEmail()
    {
        await using var ctx = CreateContext();
        var sku = new Sku
        {
            Id = Guid.NewGuid(),
            Name = "Jeu 1",
            SkuCode = "SKU-1",
            Price = 100m,
            Currency = "EUR",
            StockTotal = 10,
            ReservedQty = 1,
            Active = true
        };
        ctx.Skus.Add(sku);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            Status = "pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        ctx.Reservations.Add(reservation);
        await ctx.SaveChangesAsync();

        ctx.ReservationItems.Add(new ReservationItem
        {
            ReservationId = reservation.Id,
            SkuId = sku.Id,
            Qty = 1
        });
        await ctx.SaveChangesAsync();

        var secret = "whsec_test_123";
        var payload = BuildCheckoutCompletedPayload(reservation.Id);
        var signature = GenerateStripeSignature(payload, secret);

        var emailService = new Mock<IOrderEmailService>();
        Guid? emailedOrderId = null;
        emailService
            .Setup(s => s.SendOrderConfirmationAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string?, string?, string?, bool, CancellationToken>((orderId, _, _, _, _, _) => emailedOrderId = orderId)
            .ReturnsAsync(new EmailLogDto(Guid.NewGuid(), null, "client@example.com", "Sujet", "msg-1", "sent", null, DateTimeOffset.UtcNow));

        var logger = new Mock<ILogger<WebhooksController>>();

        var controller = new WebhooksController(
            ctx,
            emailService.Object,
            Options.Create(new StripeOptions { WebhookSecret = secret }),
            logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var request = controller.ControllerContext.HttpContext.Request;
        var bytes = Encoding.UTF8.GetBytes(payload);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        request.Headers["Stripe-Signature"] = signature;
        request.ContentType = "application/json";

        var result = await controller.Stripe(CancellationToken.None);

        Assert.IsType<OkResult>(result);

        var webhook = await ctx.WebhookEventsRaw.SingleAsync();
        Assert.Equal("evt_test_123", webhook.EventId);
        Assert.Equal("checkout.session.completed", webhook.Type);

        var order = await ctx.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal("paid", order.Status);
        Assert.Equal(reservation.Id, order.ReservationId);
        Assert.Equal("client@example.com", order.Email);
        Assert.Equal("EUR", order.Currency);
        Assert.Equal(100m, order.AmountSubtotal);
        Assert.Equal(5m, order.AmountShipping);
        Assert.Equal(20m, order.AmountTax);
        Assert.Equal(0m, order.AmountDiscount);
        Assert.Equal(125m, order.AmountTotal);
        Assert.Single(order.Items);
        Assert.Equal(sku.Id, order.Items[0].SkuId);
        Assert.Equal(1, order.Items[0].Qty);
        Assert.Equal(sku.Name, order.Items[0].NameSnapshot);
        Assert.Equal(sku.Price, order.Items[0].UnitPrice);

        var payment = await ctx.Payments.SingleAsync();
        Assert.Equal(order.Id, payment.OrderId);
        Assert.Equal("stripe", payment.Provider);
        Assert.Equal("pi_test_123", payment.ProviderPaymentId);
        Assert.Equal("succeeded", payment.Status);
        Assert.Equal(125m, payment.Amount);

        var reservationReloaded = await ctx.Reservations.SingleAsync(r => r.Id == reservation.Id);
        Assert.Equal("confirmed", reservationReloaded.Status);

        var skuReloaded = await ctx.Skus.SingleAsync(s => s.Id == sku.Id);
        Assert.Equal(0, skuReloaded.ReservedQty);
        Assert.Equal(1, skuReloaded.SoldQty);

        Assert.NotNull(emailedOrderId);
        Assert.Equal(order.Id, emailedOrderId);
        emailService.Verify(s => s.SendOrderConfirmationAsync(order.Id, null, null, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static string BuildCheckoutCompletedPayload(Guid reservationId)
    {
        var session = new
        {
            id = "cs_test_123",
            @object = "checkout.session",
            client_reference_id = reservationId.ToString(),
            payment_intent = "pi_test_123",
            payment_status = "paid",
            amount_total = 12500,
            amount_subtotal = 10000,
            currency = "eur",
            total_details = new
            {
                amount_discount = 0,
                amount_tax = 2000,
                amount_shipping = 500
            },
            customer_email = "client@example.com",
            customer_details = new
            {
                email = "client@example.com"
            },
            metadata = new Dictionary<string, string>
            {
                ["order_number"] = "OJ-2025-0001"
            }
        };

        var stripeEvent = new
        {
            id = "evt_test_123",
            @object = "event",
            api_version = "2022-11-15",
            type = "checkout.session.completed",
            data = new
            {
                @object = session
            }
        };

        return JsonSerializer.Serialize(stripeEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        });
    }

    private static string GenerateStripeSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }
}
