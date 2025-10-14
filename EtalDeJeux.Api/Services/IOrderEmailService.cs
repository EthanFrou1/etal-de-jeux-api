using EtalDeJeux.Api.Contracts.Dtos;

namespace EtalDeJeux.Api.Services;

public interface IOrderEmailService
{
    Task<EmailLogDto> SendOrderConfirmationAsync(Guid orderId, string? toEmailOverride, string? subjectOverride, string? bodyOverride, bool isHtml, CancellationToken ct);
}
