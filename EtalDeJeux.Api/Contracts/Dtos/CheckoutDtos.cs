using static EtalDeJeux.Api.Controllers.CheckoutController;

namespace EtalDeJeux.Api.Contracts.Dtos
{
    public class CheckoutDtos
    {
        public record CreateCheckoutSessionDto(Guid ReservationId, string? Email);
        public record CheckoutSessionResponseDto(string Url);


        public record CheckoutItem(
            Guid SkuId,
            int Qty,
            bool IsDigital
        );

        public record ShippingAddressDto(
            string AddressLine1,
            string? AddressLine2,
            string PostalCode,
            string City,
            string Country
        );

        public record CreateCheckoutSessionRequest(
            List<CheckoutItem> Items,
            string Email,
            string? FirstName,
            string? LastName,
            string? Phone,
            string SuccessUrl,
            string CancelUrl,
            ShippingAddressDto? ShippingAddress
        );

        public record CreateCheckoutSessionResponse(
            string Url
        );
    }
}
