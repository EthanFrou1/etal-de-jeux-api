using EtalDeJeux.Api.Contracts.Dtos;

namespace EtalDeJeux.Api.Services;

public interface IOrderEmailService
{
    Task<EmailLogDto> SendOrderConfirmationAsync(Guid orderId, SendOrderEmailDto? overrides, CancellationToken ct);
}
