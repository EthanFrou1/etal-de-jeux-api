using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    public WebhooksController(AppDbContext db) => _db = db;

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe([FromBody] object payloadObj, CancellationToken ct)
    {
        var payload = payloadObj?.ToString() ?? "{}";

        // TODO: vérification de signature Stripe si tu veux
        // Parse JSON pour extraire: id, type, data.object (session)
        var eventId = Guid.NewGuid().ToString(); // remplace par l'id réel Stripe
        var type = "checkout.session.completed"; // remplace par le type réel

        var exists = await _db.WebhookEventsRaw.AnyAsync(w => w.EventId == eventId, ct);
        if (exists) return Ok();

        _db.WebhookEventsRaw.Add(new WebhookEventRaw
        {
            EventId = eventId,
            Type = type,
            Payload = payload
        });
        await _db.SaveChangesAsync(ct);

        // TODO: construire Order/Payment via données de la session Stripe
        return Ok();
    }
}
