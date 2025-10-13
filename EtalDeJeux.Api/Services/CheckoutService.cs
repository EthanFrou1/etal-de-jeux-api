using EtalDeJeux.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly AppDbContext _db;
        public CheckoutService(AppDbContext db) => _db = db;

        public async Task<string> CreateCheckoutSessionUrlAsync(Guid reservationId, string? email, CancellationToken ct)
        {
            var res = await _db.Reservations
                .Include(r => r.Items)
                .SingleAsync(r => r.Id == reservationId, ct);

            if (res.Status != "pending") throw new InvalidOperationException("Reservation invalide");

            var skuIds = res.Items.Select(i => i.SkuId).ToList();
            var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToListAsync(ct);

            // Construire line items Stripe à partir des skus + qty
            // var lineItems = ...

            // TODO: Stripe Checkout Session create(...) — mets reservationId en client_reference_id
            // retourne l'URL
            return "https://checkout.stripe.com/test_dummy";
        }
    }

}
