namespace EtalDeJeux.Api.Services
{
    public interface ICheckoutService
    {
        Task<string> CreateCheckoutSessionUrlAsync(Guid reservationId, string? email, CancellationToken ct);
    }

}
