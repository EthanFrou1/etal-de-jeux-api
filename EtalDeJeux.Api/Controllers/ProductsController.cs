using EtalDeJeux.Api.Contracts;
using EtalDeJeux.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtalDeJeux.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // GET /api/products?search=&category=&page=1
    [HttpGet]
    public async Task<ActionResult<PaginatedProductsDto>> GetMany(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1)
    {
        const int pageSize = 12;
        page = page < 1 ? 1 : page;

        var q = db.Products.AsNoTracking()
            .Include(p => p.Skus)
            .Where(p => p.Active);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                (!string.IsNullOrEmpty(p.Description) && EF.Functions.ILike(p.Description!, $"%{search}%")));

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category == category);

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToDto())
            .ToListAsync();

        return Ok(new PaginatedProductsDto(items, total, page));
    }

    // GET /api/products/{slug}
    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDto>> GetOne(string slug)
    {
        var p = await db.Products.AsNoTracking()
            .Include(x => x.Skus)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Active);

        if (p is null)
            return NotFound(new { error = "Product not found" });

        return Ok(p.ToDto());
    }
}
