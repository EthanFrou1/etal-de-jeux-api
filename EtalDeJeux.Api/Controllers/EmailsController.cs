using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Contracts.Mapping;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api")]
public class EmailsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderEmailService _orderEmailService;

    public EmailsController(AppDbContext db, IOrderEmailService orderEmailService)
    {
        _db = db;
        _orderEmailService = orderEmailService;
    }

    [HttpPost("orders/{orderId:guid}/emails/confirm")]
    public async Task<ActionResult<EmailLogDto>> SendOrderConfirmation(Guid orderId, [FromBody] SendOrderEmailDto? dto, CancellationToken ct)
    {
        var payload = dto ?? new SendOrderEmailDto();

        try
        {
            var result = await _orderEmailService.SendOrderConfirmationAsync(orderId, payload, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("orders/{orderId:guid}/emails")]
    public async Task<ActionResult<IEnumerable<EmailLogDto>>> GetOrderEmails(Guid orderId, CancellationToken ct)
    {
        var orderExists = await _db.Orders.AnyAsync(o => o.Id == orderId, ct);
        if (!orderExists)
        {
            return NotFound(new { error = "Commande introuvable" });
        }

        var logs = await _db.EmailLogs
            .Where(l => l.OrderId == orderId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return Ok(logs);
    }

    [HttpGet("email-logs")]
    public async Task<ActionResult<PaginatedEmailLogsDto>> GetEmailLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? q = null, CancellationToken ct = default)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { error = "Paramètres de pagination invalides" });
        }

        var query = _db.EmailLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(l => EF.Functions.ILike(l.ToEmail, $"%{term}%") || EF.Functions.ILike(l.Subject, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return Ok(new PaginatedEmailLogsDto(items, total, page));
    }
}
