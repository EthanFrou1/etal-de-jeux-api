namespace EtalDeJeux.Api.Contracts.Dtos;

public record CreateReservationItemDto(Guid SkuId, int Qty);

public record CreateReservationDto(string? Email, List<CreateReservationItemDto> Items, int? HoldMinutes);

public record ReservationResponseDto(Guid ReservationId, DateTimeOffset ExpiresAt);
