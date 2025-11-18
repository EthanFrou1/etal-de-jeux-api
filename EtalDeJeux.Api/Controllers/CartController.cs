using EtalDeJeux.Api.Contracts.Dtos;
using EtalDeJeux.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<CartController> _logger;

    public CartController(AppDbContext db, ILogger<CartController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidateCartResponse>> ValidateCart(
        [FromBody] ValidateCartRequest request,
        CancellationToken ct)
    {
        try
        {
            var enrichedItems = new List<EnrichedCartItem>();
            decimal subtotal = 0;
            bool hasStockIssues = false;

            foreach (var item in request.Items)
            {
                // Récupérer le SKU avec le produit associé
                var sku = await _db.Skus
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s =>
                        s.Id == Guid.Parse(item.SkuId) &&
                        s.Active, ct);

                if (sku == null)
                {
                    _logger.LogWarning($"SKU {item.SkuId} introuvable ou inactif");
                    continue;
                }

                // Gestion spéciale pour les produits digitaux (stock = 999)
                bool isDigital = sku.StockTotal == 999;
                var inStock = isDigital || sku.StockTotal >= item.Qty;
                var actualStock = sku.StockTotal - (isDigital ? 0 : sku.SoldQty);

                if (!inStock) hasStockIssues = true;

                string? stockWarning = null;
                if (!isDigital)
                {
                    if (actualStock <= 5 && actualStock > 0)
                    {
                        stockWarning = $"Plus que {actualStock} en stock";
                    }
                    else if (sku.StockTotal == 0)
                    {
                        stockWarning = "Rupture de stock";
                    }
                }

                var enrichedItem = new EnrichedCartItem(
                    SkuId: sku.Id.ToString(),
                    ProductSlug: sku.Product.Slug,
                    Name: $"{sku.Product.Name}{(string.IsNullOrEmpty(sku.Name) ? "" : $" - {sku.Name}")}",
                    null,
                    UnitPrice: sku.Price,
                    RequestedQty: item.Qty,
                    MaxStock: actualStock,
                    InStock: inStock,
                    IsDigital: isDigital,
                    StockWarning: stockWarning
                );

                enrichedItems.Add(enrichedItem);
                subtotal += sku.Price * Math.Min(item.Qty, sku.SoldQty);
            }

            return Ok(new ValidateCartResponse(
                Items: enrichedItems,
                Subtotal: subtotal,
                HasStockIssues: hasStockIssues
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation du panier");
            return BadRequest(new { error = "Erreur lors de la validation du panier" });
        }
    }
}