using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EtalDeJeux.Api.Tests;

public class OrderEmailServiceTests
{
    [Fact]
    public async Task SendOrderConfirmationAsync_WithDefaults_SendsEmailAndLogs()
    {
        // Arrange
        using var ctx = CreateContext();
        var order = await SeedOrderAsync(ctx);
        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(order.Email, It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-123", null));

        var service = new OrderEmailService(ctx, senderMock.Object);

        // Act
        var result = await service.SendOrderConfirmationAsync(order.Id, null, null, null, true, CancellationToken.None);

        // Assert
        Assert.Equal("sent", result.Status);
        Assert.Equal("msg-123", result.MessageId);
        Assert.Equal(order.Email, result.ToEmail);
        Assert.Contains(order.OrderNumber, result.Subject);

        var log = await ctx.EmailLogs.SingleAsync();
        Assert.Equal("sent", log.Status);
        Assert.NotNull(log.Body);

        var evt = await ctx.OrderEvents.SingleAsync();
        Assert.Equal("email_confirmation_sent", evt.Type);
        Assert.Contains("msg-123", evt.Payload);
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_WhenSenderFails_LogsFailure()
    {
        using var ctx = CreateContext();
        var order = await SeedOrderAsync(ctx);
        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync(order.Email, It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "smtp down"));

        var service = new OrderEmailService(ctx, senderMock.Object);

        var result = await service.SendOrderConfirmationAsync(order.Id, null, null, null, true, CancellationToken.None);

        Assert.Equal("failed", result.Status);
        Assert.Equal("smtp down", result.Error);

        var log = await ctx.EmailLogs.SingleAsync();
        Assert.Equal("failed", log.Status);
        Assert.Equal("smtp down", log.Error);

        var evt = await ctx.OrderEvents.SingleAsync();
        Assert.Contains("failed", evt.Payload);
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_WithOverrides_UsesOverrides()
    {
        using var ctx = CreateContext();
        var order = await SeedOrderAsync(ctx);
        var senderMock = new Mock<IEmailSender>();
        senderMock.Setup(s => s.SendAsync("custom@domain.test", "Sujet", "Body", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-override", null));

        var service = new OrderEmailService(ctx, senderMock.Object);

        var result = await service.SendOrderConfirmationAsync(order.Id, "custom@domain.test", "Sujet", "Body", false, CancellationToken.None);

        Assert.Equal("custom@domain.test", result.ToEmail);
        Assert.Equal("Sujet", result.Subject);
        Assert.Equal("sent", result.Status);

        var log = await ctx.EmailLogs.SingleAsync();
        Assert.Equal("Body", log.Body);
        Assert.Equal("msg-override", log.MessageId);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Order> SeedOrderAsync(AppDbContext ctx)
    {
        var order = new Order
        {
            OrderNumber = "OJ-2025-0001",
            Email = "client@example.com",
            AmountSubtotal = 100m,
            AmountDiscount = 0m,
            AmountShipping = 5m,
            AmountTax = 20m,
            AmountTotal = 125m,
            Items =
            [
                new OrderItem { NameSnapshot = "Jeu 1", Qty = 1, UnitPrice = 100m }
            ]
        };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        return order;
    }
}
