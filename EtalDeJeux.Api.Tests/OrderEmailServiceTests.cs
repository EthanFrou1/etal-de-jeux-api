using System.Linq;
using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using EtalDeJeux.Api.Options;
using EtalDeJeux.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        senderMock.SetupSequence(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-123", null))
            .ReturnsAsync((true, "msg-owner", null));

        var service = new OrderEmailService(
            ctx,
            senderMock.Object,
            Options.Create(new OrderEmailOptions { OwnerEmail = "owner@example.com" }),
            Options.Create(new SmtpOptions { FromEmail = "from@example.com" }));

        // Act
        var result = await service.SendOrderConfirmationAsync(order.Id, null, CancellationToken.None);

        // Assert
        Assert.Equal("sent", result.Status);
        Assert.Equal("msg-123", result.MessageId);
        Assert.Equal(order.Email, result.ToEmail);
        Assert.Contains(order.OrderNumber, result.Subject);

        var logs = await ctx.EmailLogs.OrderBy(l => l.CreatedAt).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ToEmail == order.Email && l.Status == "sent");
        Assert.Contains(logs, l => l.ToEmail == "owner@example.com" && l.Status == "sent");

        var events = await ctx.OrderEvents.OrderBy(e => e.CreatedAt).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Type == "email_confirmation_sent");
        Assert.Contains(events, e => e.Type == "email_confirmation_sent_owner");

        senderMock.Verify(s => s.SendAsync(order.Email, It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(s => s.SendAsync("owner@example.com", It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_WhenSenderFails_LogsFailure()
    {
        using var ctx = CreateContext();
        var order = await SeedOrderAsync(ctx);
        var senderMock = new Mock<IEmailSender>();
        senderMock.SetupSequence(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "smtp down"))
            .ReturnsAsync((true, "msg-owner", null));

        var service = new OrderEmailService(
            ctx,
            senderMock.Object,
            Options.Create(new OrderEmailOptions { OwnerEmail = "owner@example.com" }),
            Options.Create(new SmtpOptions { FromEmail = "from@example.com" }));

        var result = await service.SendOrderConfirmationAsync(order.Id, null, CancellationToken.None);

        Assert.Equal("failed", result.Status);
        Assert.Equal("smtp down", result.Error);

        var logs = await ctx.EmailLogs.OrderBy(l => l.CreatedAt).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ToEmail == order.Email && l.Status == "failed" && l.Error == "smtp down");
        Assert.Contains(logs, l => l.ToEmail == "owner@example.com" && l.Status == "sent");

        var events = await ctx.OrderEvents.OrderBy(e => e.CreatedAt).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Type == "email_confirmation_sent" && e.Payload.Contains("failed"));
        Assert.Contains(events, e => e.Type == "email_confirmation_sent_owner");
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_WithOverrides_UsesOverrides()
    {
        using var ctx = CreateContext();
        var order = await SeedOrderAsync(ctx);
        var senderMock = new Mock<IEmailSender>();
        senderMock.SetupSequence(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "msg-override", null))
            .ReturnsAsync((true, "msg-owner", null));

        var service = new OrderEmailService(
            ctx,
            senderMock.Object,
            Options.Create(new OrderEmailOptions { OwnerEmail = "owner@example.com" }),
            Options.Create(new SmtpOptions { FromEmail = "from@example.com" }));

        var result = await service.SendOrderConfirmationAsync(
            order.Id,
            new SendOrderEmailDto
            {
                ToEmail = "custom@domain.test",
                SubjectOverride = "Sujet",
                BodyOverride = "Body",
                IsHtml = false,
                Owner = new OrderEmailRecipientDto
                {
                    ToEmail = "backoffice@example.com",
                    SubjectOverride = "Backoffice",
                    BodyOverride = "OwnerBody",
                    IsHtml = false
                }
            },
            CancellationToken.None);

        Assert.Equal("custom@domain.test", result.ToEmail);
        Assert.Equal("Sujet", result.Subject);
        Assert.Equal("sent", result.Status);

        var logs = await ctx.EmailLogs.OrderBy(l => l.CreatedAt).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ToEmail == "custom@domain.test" && l.Body == "Body" && l.MessageId == "msg-override");
        Assert.Contains(logs, l => l.ToEmail == "backoffice@example.com" && l.Body == "OwnerBody");

        senderMock.Verify(s => s.SendAsync("custom@domain.test", "Sujet", "Body", false, It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(s => s.SendAsync("backoffice@example.com", "Backoffice", "OwnerBody", false, It.IsAny<CancellationToken>()), Times.Once);
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
