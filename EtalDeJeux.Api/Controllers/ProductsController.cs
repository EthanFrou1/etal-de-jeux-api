using EtalDeJeux.Api.Contracts;
using EtalDeJeux.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedProductsDto>> GetMany(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1)
    {
        const int pageSize = 12;
        page = page < 1 ? 1 : page;

        var query = db.Products.AsNoTracking()
            .Include(p => p.Skus)
            .Include(p => p.ProductMechanics).ThenInclude(pm => pm.Mechanic)
            .Include(p => p.ProductDesigners).ThenInclude(pd => pd.Designer)
            .Include(p => p.ProductPublishers).ThenInclude(pp => pp.Publisher)
            .Include(p => p.Files)
            .Where(p => p.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                (!string.IsNullOrEmpty(p.Description) && EF.Functions.ILike(p.Description!, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PaginatedProductsDto(items.Select(p => p.ToDto()).ToList(), total, page));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDto>> GetOne(string slug)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Skus)
            .Include(p => p.ProductMechanics).ThenInclude(pm => pm.Mechanic)
            .Include(p => p.ProductDesigners).ThenInclude(pd => pd.Designer)
            .Include(p => p.ProductPublishers).ThenInclude(pp => pp.Publisher)
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Active);

        if (product is null)
        {
            return NotFound(new { error = "Product not found" });
        }

        return Ok(product.ToDto());
    }
}
