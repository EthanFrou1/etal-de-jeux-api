using EtalDeJeux.Api.Contracts.Dtos;

namespace EtalDeJeux.Api.Services;

public interface IReservationService
{
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, CancellationToken ct);
    Task ExpirePendingAsync(CancellationToken ct);
}
