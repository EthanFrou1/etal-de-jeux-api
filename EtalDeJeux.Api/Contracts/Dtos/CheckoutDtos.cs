namespace EtalDeJeux.Api.Contracts.Dtos
{
    public class CheckoutDtos
    {
        public record CreateCheckoutSessionDto(Guid ReservationId, string? Email);
        public record CheckoutSessionResponseDto(string Url);
    }
}
