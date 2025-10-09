using EtalDeJeux.Api.Models;

namespace EtalDeJeux.Api.Contracts;

public record SkuDto(Guid Id, string Name, string? SkuCode, int PriceCents, string Currency, int Stock, bool Active);
public record ProductDto(Guid Id, string Slug, string Name, string? Description, string Category, bool Active, List<string> Images, List<SkuDto> Skus);
public record PaginatedProductsDto(List<ProductDto> Items, int Total, int Page);

public static class DtoMapper
{
    public static ProductDto ToDto(this Product p) =>
        new(
            p.Id, p.Slug, p.Name, p.Description, p.Category, p.Active,
            p.Images,
            p.Skus.Select(s => new SkuDto(s.Id, s.Name, s.SkuCode, s.PriceCents, s.Currency, s.Stock, s.Active)).ToList()
        );
}
