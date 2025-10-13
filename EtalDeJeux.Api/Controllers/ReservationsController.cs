using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _svc;
    public ReservationsController(IReservationService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult<ReservationResponseDto>> Create([FromBody] CreateReservationDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _svc.CreateAsync(dto, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
