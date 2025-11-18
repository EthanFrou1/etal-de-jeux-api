namespace EtalDeJeux.Api.Models;

public class ReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid SkuId { get; set; }
    public int Qty { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public Sku Sku { get; set; } = default!;
}
