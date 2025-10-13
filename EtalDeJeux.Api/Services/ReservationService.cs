using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Data;
using EtalDeJeux.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;
    public ReservationService(AppDbContext db) => _db = db;

    public async Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, CancellationToken ct)
    {
        var minutes = dto.HoldMinutes ?? 15;

        // Vérif stock simple (existe & actif & stock_total - reserved_qty - sold_qty >= qty)
        var skuIds = dto.Items.Select(i => i.SkuId).ToList();
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToListAsync(ct);

        foreach (var item in dto.Items)
        {
            var sku = skus.Single(s => s.Id == item.SkuId);
            var available = (sku.StockTotal) - (sku.ReservedQty) - (sku.SoldQty);
            if (available < item.Qty) throw new InvalidOperationException($"Stock insuffisant pour {sku.Name}");
        }

        var res = new Reservation
        {
            Status = "pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes)
        };
        _db.Reservations.Add(res);
        await _db.SaveChangesAsync(ct);

        foreach (var item in dto.Items)
        {
            _db.ReservationItems.Add(new ReservationItem
            {
                ReservationId = res.Id,
                SkuId = item.SkuId,
                Qty = item.Qty
            });
            var sku = skus.Single(s => s.Id == item.SkuId);
            sku.ReservedQty = (sku.ReservedQty) + item.Qty;
        }

        await _db.SaveChangesAsync(ct);
        return new ReservationResponseDto(res.Id, res.ExpiresAt);
    }

    public async Task ExpirePendingAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = await _db.Reservations
            .Where(r => r.Status == "pending" && r.ExpiresAt < now)
            .Include(r => r.Items)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        var skuIds = expired.SelectMany(r => r.Items.Select(i => i.SkuId)).Distinct().ToList();
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToListAsync(ct);

        foreach (var r in expired)
        {
            r.Status = "expired";
            foreach (var it in r.Items)
            {
                var sku = skus.Single(s => s.Id == it.SkuId);
                sku.ReservedQty = Math.Max(0, (sku.ReservedQty) - it.Qty);
            }
        }
        await _db.SaveChangesAsync(ct);
    }
}
